using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Analysis;
using GoldSignalAnalyzer.Application.Freshness;
using GoldSignalAnalyzer.Application.News;
using GoldSignalAnalyzer.Application.Scoring;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Live;

/// <summary>Fixed per-session inputs for a live refresh (Cycle 7, FR-36/FR-37).</summary>
public sealed record LiveSignalOptions(
    NormalizedSymbol Symbol,
    TimeFrame TimeFrame,
    int CandleCount,
    SymbolSpec Spec,
    decimal AccountBalance,
    SignalContext Context);

/// <summary>
/// Cycle 9 (FR-43): the higher-timeframe confirmation provenance of one live refresh, so
/// the advisory decision's HTF basis is traceable (INV-5) rather than a silent side-channel.
/// It records THREE distinguishable states so the audit trail is never an ambiguous null:
///   * <c>Applicable=false</c> (top of ladder, D1) — no higher curated timeframe exists;
///   * <c>Applicable=true, Available=false</c> — an HTF read was attempted but was
///     empty/failed/tied/frozen (fail-safe → the guard blocks);
///   * <c>Applicable=true, Available=true, Direction=…</c> — a real HTF lean was derived.
/// This carrier holds NO order/execution affordance (INV-1) — its member names are HTF/
/// timeframe/direction only.
/// </summary>
public sealed record HtfConfirmation(
    bool Applicable,
    TimeFrame? TimeFrame,
    SignalDirection? Direction,
    bool Available);

/// <summary>
/// The outcome of one live refresh. Either the gate ALLOWED a signal (then
/// <see cref="Analysis"/> is present) or it SUPPRESSED one (then
/// <see cref="Analysis"/> is null and <see cref="SuppressionReason"/> is the banner
/// text). Candles are the real bars pulled from the feed (may be shown on the chart
/// even when the signal is suppressed) — never fabricated (INV-4/NFR-5).
/// </summary>
public sealed record LiveRefreshResult(
    SignalGateDecision Decision,
    SignalAnalysis? Analysis,
    IReadOnlyList<Candle> Candles,
    FreshnessAssessment Freshness,
    ConnectionState ConnectionState,
    HtfConfirmation? Htf = null)
{
    public bool SignalAllowed => Decision.IsAllowed && Analysis is not null;
    public string? SuppressionReason => Decision.IsAllowed ? null : Decision.Reason;
}

/// <summary>
/// Cycle 7 (FR-36/FR-37): the read-only bridge between a LIVE
/// <see cref="IMarketDataProvider"/> and the existing analytical core. It pulls the
/// latest tick + candles, updates freshness, runs the FR-12 veto gate, and only
/// then runs the (already-tested) <see cref="SignalAnalysisService"/>.
///
/// It computes nothing new and adds NO order surface (INV-1). Its whole job is to
/// make live data SAFE to analyse:
///   * never Connected → suppressed (CONNECTION NOT READY), no analysis;
///   * no confirmed gold mapping → suppressed (NO GOLD SYMBOL MAPPED);
///   * data not fresh (stale / future-dated / never seen) → suppressed (DATA STALE);
///   * empty candle pull → suppressed (DATA STALE) rather than analysing nothing.
/// A price is NEVER invented: on any failing condition the result carries null
/// analysis and the exact banner reason, and the app stays Neutral.
///
/// Pure/awaitable and fully unit-testable against <c>TestMarketDataProvider</c> +
/// a <c>ManualClock</c> — no real terminal required.
/// </summary>
public sealed class LiveSignalCoordinator
{
    private readonly IMarketDataProvider _provider;
    private readonly SignalAnalysisService _analysis;
    private readonly SignalGate _gate;
    private readonly DataFreshnessMonitor _freshness;
    // Cycle 9 (FR-42): derives the higher-timeframe confirmation direction from a
    // stepped-up candle series through the existing core. Cycle 9 (D9-11): the clock is
    // used ONLY for the live HTF recency bound (reject a frozen/ancient HTF feed).
    private readonly HigherTimeFrameAnalyzer _htf;
    private readonly IClock _clock;
    // Cycle 10 (FR-44): optional news-blackout gate. Null (default) preserves prior behaviour
    // exactly — no existing caller/test is affected unless it opts in.
    private readonly NewsBlackoutGate? _newsGate;
    // Cycle 8 (FR-40): the timeframe is now settable at runtime, so `_options` is no
    // longer readonly. Every refresh SNAPSHOTS it at the start (see RefreshAsync) so a
    // SetTimeFrame during an in-flight poll can never change the timeframe that poll
    // pulls/analyses — no mid-poll mislabel (INV-4).
    private LiveSignalOptions _options;
    private readonly bool _hasSymbolMapping;

    // D9-11: an HTF series whose newest candle is older than this multiple of the HTF
    // period is treated as unavailable (frozen/cached/ancient feed) → fail-safe Neutral.
    // Generous (10×) so normal weekend/market gaps never false-reject, but a week-plus-old
    // or year-old lean can never silently "confirm" a live signal.
    private const int HtfMaxAgeInPeriods = 10;

    public LiveSignalCoordinator(
        IMarketDataProvider provider,
        SignalAnalysisService analysis,
        SignalGate gate,
        DataFreshnessMonitor freshness,
        LiveSignalOptions options,
        bool hasSymbolMapping,
        IClock clock,
        HigherTimeFrameAnalyzer? htfAnalyzer = null,
        NewsBlackoutGate? newsGate = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _freshness = freshness ?? throw new ArgumentNullException(nameof(freshness));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _hasSymbolMapping = hasSymbolMapping;
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _htf = htfAnalyzer ?? new HigherTimeFrameAnalyzer();
        _newsGate = newsGate;
    }

    /// <summary>
    /// Cycle 8 (FR-40): the timeframe the NEXT refresh will pull/analyse. Not an order
    /// surface — it only chooses the candle bucket size (INV-1 intact).
    /// </summary>
    public TimeFrame CurrentTimeFrame => _options.TimeFrame;

    /// <summary>
    /// Cycle 8 (FR-40): switch the analysis timeframe at runtime. The next
    /// <see cref="RefreshAsync"/> re-pulls candles at this timeframe through the SAME
    /// FR-12 freshness/veto gate. Freshness state is deliberately NOT reset — it is keyed
    /// on tick recency (not candle timeframe), so a switch can never bypass suppression.
    /// An in-flight refresh is unaffected (it snapshots the timeframe at its start).
    /// </summary>
    public void SetTimeFrame(TimeFrame timeFrame)
        => _options = _options with { TimeFrame = timeFrame };

    public async Task<LiveRefreshResult> RefreshAsync(bool hasOpenPosition, CancellationToken ct = default)
    {
        // Snapshot the mutable options ONCE so a runtime SetTimeFrame mid-await cannot
        // change the timeframe this refresh pulls/analyses (FR-40 / INV-4).
        var options = _options;

        // 1. Not connected → suppress immediately; never touch the data calls
        //    (the live provider would throw, and we must not fabricate anyway).
        var state = _provider.State;
        if (state != ConnectionState.Connected)
        {
            var freshnessNow = _freshness.Assess();
            var decisionNow = _gate.Evaluate(state, freshnessNow, _hasSymbolMapping);
            return new LiveRefreshResult(decisionNow, null, Array.Empty<Candle>(), freshnessNow, state);
        }

        // 2. Pull a tick (best-effort) to drive freshness, then the candle window.
        MarketTick? tick = await _provider.GetLatestTickAsync(options.Symbol, ct).ConfigureAwait(false);
        if (tick is not null) _freshness.RecordTick(tick.TimestampUtc);

        IReadOnlyList<Candle> candles =
            await _provider.GetCandlesAsync(options.Symbol, options.TimeFrame, options.CandleCount, ct)
                .ConfigureAwait(false);

        // Fall back to the newest candle's time for freshness if no tick was available.
        if (tick is null && candles.Count > 0) _freshness.RecordTick(candles[^1].OpenTimeUtc);

        var freshness = _freshness.Assess();
        var decision = _gate.Evaluate(state, freshness, _hasSymbolMapping);

        // 3. Gate says no, or there is simply nothing to analyse → suppress, no fabrication.
        if (!decision.IsAllowed)
            return new LiveRefreshResult(decision, null, candles, freshness, state);
        if (candles.Count == 0)
            return new LiveRefreshResult(
                new SignalGateDecision(SignalGateStatus.Suppressed, SignalGate.StaleReason),
                null, candles, freshness, state);

        // 4. Gate allowed and we have real, fresh candles. Derive a REAL higher-timeframe
        //    confirmation (Cycle 9, FR-42) before running the core, so the FR-21 HTF guard
        //    performs a genuine check. The HTF timeframe is StepUp of the SNAPSHOTTED
        //    timeframe (options.TimeFrame), so a mid-poll SetTimeFrame cannot leak a stale
        //    HTF read into this poll (INV-4 / D9-7).
        var htf = await DeriveHtfAsync(options, ct).ConfigureAwait(false);
        // Cycle 10 (FR-44): a pure, clock-driven calendar check — no network call, cannot itself
        // fail/throw, so it needs no try/catch (unlike the HTF pull, which touches the provider).
        bool newsBlackout = _newsGate?.ActiveBlackout(_clock.UtcNow) is not null;
        var ctx = options.Context with
        {
            HtfConfirmationApplicable = htf.Applicable,
            HtfDirection = htf.Direction,
            NewsBlackout = newsBlackout,
        };

        var analysis = _analysis.Analyze(
            options.Symbol, options.TimeFrame, candles, options.Spec,
            options.AccountBalance, ctx, hasOpenPosition);

        return new LiveRefreshResult(decision, analysis, candles, freshness, state, htf);
    }

    /// <summary>
    /// Cycle 9 (FR-42/FR-43/D9-4/D9-11): pull the higher-timeframe series and derive its
    /// directional lean. Fail-safe by construction:
    ///   * top of ladder (StepUp == null, i.e. D1) → not applicable, guard becomes a no-op;
    ///   * an HTF pull that throws/empties → unavailable → null direction → guard blocks;
    ///   * an HTF series older than <see cref="HtfMaxAgeInPeriods"/> × its period (frozen/
    ///     ancient feed) → unavailable → guard blocks.
    /// The try/catch is scoped to the HTF pull ONLY (Auditor F1) so an HTF failure degrades
    /// to a clean guard-Neutral and never escapes to the caller's generic error handler,
    /// which would mislabel a perfectly good LTF poll as a "feed error". The HTF pull never
    /// touches the freshness monitor (IC-8) — LTF staleness stays uncorrupted.
    /// </summary>
    private async Task<HtfConfirmation> DeriveHtfAsync(LiveSignalOptions options, CancellationToken ct)
    {
        var htfTf = TimeFrameLadder.StepUp(options.TimeFrame);
        if (htfTf is null)
            return new HtfConfirmation(Applicable: false, TimeFrame: null, Direction: null, Available: false);

        IReadOnlyList<Candle> htfCandles;
        try
        {
            htfCandles = await _provider
                .GetCandlesAsync(options.Symbol, htfTf.Value, options.CandleCount, ct)
                .ConfigureAwait(false);
        }
        catch
        {
            // Unavailable HTF read → fail-safe (never fabricate, never crash the poll).
            return new HtfConfirmation(Applicable: true, TimeFrame: htfTf.Value, Direction: null, Available: false);
        }

        // D9-11: live-only recency bound — reject a frozen/ancient HTF feed.
        if (htfCandles.Count > 0)
        {
            var newest = htfCandles[^1].OpenTimeUtc;
            var maxAge = TimeSpan.FromMinutes((int)htfTf.Value * HtfMaxAgeInPeriods);
            if (_clock.UtcNow - newest > maxAge)
                return new HtfConfirmation(Applicable: true, TimeFrame: htfTf.Value, Direction: null, Available: false);
        }

        var direction = _htf.Confirm(options.Symbol, htfTf.Value, htfCandles);
        return new HtfConfirmation(
            Applicable: true, TimeFrame: htfTf.Value, Direction: direction, Available: direction is not null);
    }
}
