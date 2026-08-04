using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Analysis;
using GoldSignalAnalyzer.Application.Freshness;
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
    ConnectionState ConnectionState)
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
    private readonly LiveSignalOptions _options;
    private readonly bool _hasSymbolMapping;

    public LiveSignalCoordinator(
        IMarketDataProvider provider,
        SignalAnalysisService analysis,
        SignalGate gate,
        DataFreshnessMonitor freshness,
        LiveSignalOptions options,
        bool hasSymbolMapping)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _freshness = freshness ?? throw new ArgumentNullException(nameof(freshness));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _hasSymbolMapping = hasSymbolMapping;
    }

    public async Task<LiveRefreshResult> RefreshAsync(bool hasOpenPosition, CancellationToken ct = default)
    {
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
        MarketTick? tick = await _provider.GetLatestTickAsync(_options.Symbol, ct).ConfigureAwait(false);
        if (tick is not null) _freshness.RecordTick(tick.TimestampUtc);

        IReadOnlyList<Candle> candles =
            await _provider.GetCandlesAsync(_options.Symbol, _options.TimeFrame, _options.CandleCount, ct)
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

        // 4. Gate allowed and we have real, fresh candles → run the tested core.
        var analysis = _analysis.Analyze(
            _options.Symbol, _options.TimeFrame, candles, _options.Spec,
            _options.AccountBalance, _options.Context, hasOpenPosition);

        return new LiveRefreshResult(decision, analysis, candles, freshness, state);
    }
}
