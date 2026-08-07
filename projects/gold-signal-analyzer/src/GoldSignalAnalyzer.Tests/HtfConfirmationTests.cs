using GoldSignalAnalyzer.Application.Analysis;
using GoldSignalAnalyzer.Application.Freshness;
using GoldSignalAnalyzer.Application.Indicators;
using GoldSignalAnalyzer.Application.Live;
using GoldSignalAnalyzer.Application.Scoring;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Presentation;
using GoldSignalAnalyzer.Testing;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// Cycle 9 (FR-41/FR-42/FR-43) — the real higher-timeframe (HTF) confirmation fix.
/// Proves the FR-21 guard now performs a genuine check on BOTH paths, that it fails safe,
/// that the D1 top-of-ladder no-op is set ONLY at D1, that the HTF read rides the
/// start-of-refresh snapshot, and that the audit trail records HTF provenance without
/// fabrication.
/// </summary>
public class HtfConfirmationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    /// <summary>A clean directional series at <paramref name="tf"/> ending at <paramref name="end"/>.</summary>
    private static IReadOnlyList<Candle> Series(TimeFrame tf, bool up, int n = 80, DateTimeOffset? end = null)
    {
        int minutes = (int)tf;
        var endOpen = end ?? Now;
        var start = endOpen.AddMinutes(-minutes * (n - 1));
        var list = new List<Candle>(n);
        const decimal basePrice = 1900m;
        for (int i = 0; i < n; i++)
        {
            decimal open = up ? basePrice + i * 1.5m : basePrice - i * 1.5m;
            decimal close = up ? open + 1.0m : open - 1.0m;
            decimal high = Math.Max(open, close) + 0.5m;
            decimal low = Math.Min(open, close) - 0.5m;
            list.Add(new Candle(NormalizedSymbol.Gold, tf, start.AddMinutes(minutes * i), open, high, low, close, 1000 + i));
        }
        return list;
    }

    /// <summary>A perfectly flat series → no directional edge (RSI 50, Stoch 50, MACD 0, …)
    /// → BuyScore == SellScore == 0 → a genuine tie.</summary>
    private static IReadOnlyList<Candle> Flat(TimeFrame tf, int n = 80)
    {
        int minutes = (int)tf;
        var start = Now.AddMinutes(-minutes * (n - 1));
        var list = new List<Candle>(n);
        for (int i = 0; i < n; i++)
            list.Add(new Candle(NormalizedSymbol.Gold, tf, start.AddMinutes(minutes * i), 1900m, 1900.5m, 1899.5m, 1900m, 1000));
        return list;
    }

    // ===================== FR-41: TimeFrameLadder.StepUp =====================

    [Theory] // AC-41.1: each rung maps to the next; D1 and any unmapped value → null.
    [InlineData(TimeFrame.M1, TimeFrame.M5)]
    [InlineData(TimeFrame.M5, TimeFrame.M15)]
    [InlineData(TimeFrame.M15, TimeFrame.M30)]
    [InlineData(TimeFrame.M30, TimeFrame.H1)]
    [InlineData(TimeFrame.H1, TimeFrame.H4)]
    [InlineData(TimeFrame.H4, TimeFrame.D1)]
    public void StepUp_maps_each_rung(TimeFrame from, TimeFrame expected)
        => Assert.Equal(expected, TimeFrameLadder.StepUp(from));

    [Fact] // AC-41.1: top of ladder (D1) and an unmapped enum value → null (no higher timeframe).
    public void StepUp_returns_null_at_top_and_for_unknown()
    {
        Assert.Null(TimeFrameLadder.StepUp(TimeFrame.D1));
        Assert.Null(TimeFrameLadder.StepUp((TimeFrame)999));
    }

    // ===================== FR-41: HigherTimeFrameAnalyzer.Confirm =====================

    [Theory] // AC-41.2: the derived lean follows the HTF series direction …
    [InlineData(true, SignalDirection.Buy)]
    [InlineData(false, SignalDirection.Sell)]
    public void Confirm_returns_direction_of_htf_lean(bool up, SignalDirection expected)
    {
        var candles = Series(TimeFrame.H4, up);
        var actual = new HigherTimeFrameAnalyzer().Confirm(NormalizedSymbol.Gold, TimeFrame.H4, candles);
        Assert.Equal(expected, actual);

        // … and it equals sign(BuyScore − SellScore) computed INDEPENDENTLY on the same
        // candles (the same basis SignalClassifier uses for `proposed`), not "does not throw".
        var snap = new IndicatorEngine().Compute(NormalizedSymbol.Gold, TimeFrame.H4, candles);
        var regime = new RegimeClassifier().Classify(snap);
        var score = new ScoringEngine().Score(snap, candles[^1].Close, regime);
        SignalDirection? independent = score.BuyScore > score.SellScore ? SignalDirection.Buy
            : score.SellScore > score.BuyScore ? SignalDirection.Sell : null;
        Assert.Equal(independent, actual);
    }

    [Fact] // AC-41.3: no usable series (null or empty) → null (fail-safe, no fabrication).
    public void Confirm_returns_null_when_no_htf_series()
    {
        var a = new HigherTimeFrameAnalyzer();
        Assert.Null(a.Confirm(NormalizedSymbol.Gold, TimeFrame.H4, null));
        Assert.Null(a.Confirm(NormalizedSymbol.Gold, TimeFrame.H4, Array.Empty<Candle>()));
    }

    [Fact] // AC-41.4: a flat series with no directional edge → tie → null.
    public void Confirm_returns_null_on_tie()
        => Assert.Null(new HigherTimeFrameAnalyzer().Confirm(NormalizedSymbol.Gold, TimeFrame.H4, Flat(TimeFrame.H4)));

    [Fact] // D9-10: a warm series yields a real (non-null) direction …
    public void Confirm_returns_direction_when_series_meets_warmup()
        => Assert.Equal(SignalDirection.Buy,
            new HigherTimeFrameAnalyzer().Confirm(NormalizedSymbol.Gold, TimeFrame.H4, Series(TimeFrame.H4, up: true, n: 50)));

    [Fact] // D9-10: a series BELOW the longest indicator warmup → all scores 0 → tie → null.
    // This is the exact "insufficient bars → guard blocks forever" failure the fix must make a
    // DELIBERATE, asserted fail-safe (not an accident). The live/sample HTF pulls use 120/80 bars.
    public void Confirm_returns_null_when_series_below_indicator_warmup()
        => Assert.Null(new HigherTimeFrameAnalyzer().Confirm(NormalizedSymbol.Gold, TimeFrame.H4, Series(TimeFrame.H4, up: true, n: 8)));

    // ===================== FR-42: the classifier guard =====================

    private static ScoreResult ScoreOf(decimal buy, decimal sell, MarketRegime regime = MarketRegime.TrendingUp)
        => new(buy, sell, regime, new ScoringConfig().TrendingWeights,
               Array.Empty<ScoreContribution>(), Array.Empty<Penalty>(), Array.Empty<Veto>());

    [Theory] // AC-42.1 (both directions): the guard BLOCKS when the HTF is unconfirmed/conflicting
             // and PASSES only when it is aligned. The null case is the exact live bug.
    [InlineData(SignalDirection.Buy)]
    [InlineData(SignalDirection.Sell)]
    public void Htf_guard_blocks_when_unconfirmed_passes_when_aligned(SignalDirection proposed)
    {
        var score = proposed == SignalDirection.Buy
            ? ScoreOf(70m, 10m, MarketRegime.TrendingUp)
            : ScoreOf(10m, 70m, MarketRegime.TrendingDown);
        var opposite = proposed == SignalDirection.Buy ? SignalDirection.Sell : SignalDirection.Buy;

        SignalClassification Classify(SignalDirection? htf) =>
            new SignalClassifier(new ManualClock(Now))
                .Classify(score, new SignalContext(HtfDirection: htf, HtfConfirmationApplicable: true));

        // null (never populated) → Neutral(ReasonHtf) — the live bug, now caught.
        Assert.Equal(SignalClassifier.ReasonHtf, Classify(null).PrimaryReason);
        // conflicting HTF → Neutral(ReasonHtf).
        Assert.Equal(SignalClassifier.ReasonHtf, Classify(opposite).PrimaryReason);
        // aligned HTF → actionable in the proposed direction.
        var aligned = Classify(proposed);
        Assert.Equal(proposed, aligned.Direction);
        Assert.True(aligned.IsActionable);
    }

    [Fact] // AC-42.2: at the top of the ladder the guard is a no-op — a signal fires even with a
    // null (or conflicting) HTF, because HTF confirmation is not applicable at D1.
    public void Htf_guard_is_noop_at_top_of_ladder()
    {
        var score = ScoreOf(70m, 10m);
        var clock = new ManualClock(Now);
        Assert.Equal(SignalDirection.Buy,
            new SignalClassifier(clock).Classify(score,
                new SignalContext(HtfDirection: null, HtfConfirmationApplicable: false)).Direction);
        Assert.Equal(SignalDirection.Buy,
            new SignalClassifier(new ManualClock(Now)).Classify(score,
                new SignalContext(HtfDirection: SignalDirection.Sell, HtfConfirmationApplicable: false)).Direction);
    }

    [Theory] // IC-4: the sample producer sets HtfConfirmationApplicable=false ONLY at D1 — a bug
    // that disabled the guard on any other timeframe would fail here.
    [InlineData(TimeFrame.M1)]
    [InlineData(TimeFrame.M5)]
    [InlineData(TimeFrame.M15)]
    [InlineData(TimeFrame.M30)]
    [InlineData(TimeFrame.H1)]
    [InlineData(TimeFrame.H4)]
    [InlineData(TimeFrame.D1)]
    public void HtfConfirmationApplicable_is_false_only_at_top_of_ladder(TimeFrame tf)
        => Assert.Equal(tf != TimeFrame.D1, SampleHtfContext.For(tf).HtfConfirmationApplicable);

    [Theory] // AC-42.6: the sample context's HtfDirection matches the analyzer's derivation on
    // the stepped-up sample series (parity with live), for every non-top curated timeframe.
    [InlineData(TimeFrame.M1)]
    [InlineData(TimeFrame.H1)]
    [InlineData(TimeFrame.H4)]
    public void Sample_htf_context_matches_analyzer(TimeFrame tf)
    {
        var htfTf = TimeFrameLadder.StepUp(tf)!.Value;
        var expected = new HigherTimeFrameAnalyzer().Confirm(
            NormalizedSymbol.Gold, htfTf, SampleCandleSeries.Build(htfTf));
        var ctx = SampleHtfContext.For(tf);
        Assert.True(ctx.HtfConfirmationApplicable);
        Assert.Equal(expected, ctx.HtfDirection);
    }

    // ===================== FR-42: the live coordinator =====================

    private static (LiveSignalCoordinator coord, TestMarketDataProvider provider) BuildLive(
        TimeFrame ltf, IReadOnlyList<Candle> ltfCandles,
        IReadOnlyList<Candle>? htfCandles = null, bool noTick = false,
        TimeFrame? throwOnHtf = null)
    {
        var clock = new ManualClock(Now);
        var provider = new TestMarketDataProvider()
            .WithSymbols(new BrokerSymbol("XAUUSD", "Gold"))
            .WithCandles(ltf, ltfCandles);
        var htfTf = TimeFrameLadder.StepUp(ltf);
        if (htfTf is not null && htfCandles is not null) provider.WithCandles(htfTf.Value, htfCandles);
        if (throwOnHtf is not null) provider.ThrowOnTimeFrame(throwOnHtf.Value);
        if (!noTick) // default: a fresh tick at Now; noTick forces the LTF candle-fallback path
            provider.WithLatestTick(new MarketTick(NormalizedSymbol.Gold, 1990m, 1990.2m, Now));
        provider.ConnectAsync().GetAwaiter().GetResult();

        var options = new LiveSignalOptions(NormalizedSymbol.Gold, ltf, CandleCount: 80,
            SymbolSpec.Gold(), 10_000m, new SignalContext());
        var coord = new LiveSignalCoordinator(provider,
            new SignalAnalysisService(clock), new SignalGate(),
            new DataFreshnessMonitor(clock, TimeSpan.FromMinutes(90)), options, hasSymbolMapping: true, clock);
        return (coord, provider);
    }

    [Fact] // AC-42.3: LIVE path CONFIRMS — bullish LTF + bullish HTF → actionable Buy.
    // This is the direct regression for the reported live bug (before: forced Neutral forever).
    public async Task Live_refresh_confirms_with_real_htf()
    {
        var (coord, provider) = BuildLive(TimeFrame.H1,
            ltfCandles: Series(TimeFrame.H1, up: true), htfCandles: Series(TimeFrame.H4, up: true));

        var r = await coord.RefreshAsync(hasOpenPosition: false);

        Assert.True(r.SignalAllowed);
        Assert.Equal(SignalDirection.Buy, r.Analysis!.Signal.Direction);
        Assert.True(r.Analysis.Signal.IsActionable);
        Assert.NotNull(r.Htf);
        Assert.True(r.Htf!.Applicable);
        Assert.True(r.Htf.Available);
        Assert.Equal(TimeFrame.H4, r.Htf.TimeFrame);
        Assert.Equal(SignalDirection.Buy, r.Htf.Direction);
        Assert.Equal(new[] { TimeFrame.H1, TimeFrame.H4 }, provider.RequestedTimeFrames);
    }

    [Fact] // AC-42.3: LIVE path BLOCKS — bullish LTF proposal + bearish HTF → Neutral(ReasonHtf).
    // Only constructible because the HTF pull returns a DIFFERENT (bearish) series (IC-1).
    public async Task Live_refresh_blocks_on_conflicting_htf()
    {
        var (coord, _) = BuildLive(TimeFrame.H1,
            ltfCandles: Series(TimeFrame.H1, up: true), htfCandles: Series(TimeFrame.H4, up: false));

        var r = await coord.RefreshAsync(hasOpenPosition: false);

        Assert.Equal(SignalDirection.Neutral, r.Analysis!.Signal.Direction);
        Assert.Equal(SignalClassifier.ReasonHtf, r.Analysis.Signal.PrimaryReason);
        Assert.True(r.Htf!.Applicable);
        Assert.Equal(SignalDirection.Sell, r.Htf.Direction);
    }

    [Fact] // AC-42.4: LIVE fail-safe — the HTF pull THROWS → scoped catch → Neutral(ReasonHtf),
    // gate still allowed, NO exception escapes the refresh (would otherwise mislabel a good LTF
    // poll as a "feed error").
    public async Task Live_refresh_failsafe_when_htf_unavailable()
    {
        var (coord, _) = BuildLive(TimeFrame.H1,
            ltfCandles: Series(TimeFrame.H1, up: true), throwOnHtf: TimeFrame.H4);

        var r = await coord.RefreshAsync(hasOpenPosition: false); // must not throw

        Assert.NotNull(r.Analysis);
        Assert.Equal(SignalDirection.Neutral, r.Analysis!.Signal.Direction);
        Assert.Equal(SignalClassifier.ReasonHtf, r.Analysis.Signal.PrimaryReason);
        Assert.True(r.SignalAllowed);          // the LTF gate DID allow — this is not a suppression
        Assert.True(r.Htf!.Applicable);
        Assert.False(r.Htf.Available);          // HTF unavailable
        Assert.Equal(TimeFrame.H4, r.Htf.TimeFrame);
        Assert.Null(r.Htf.Direction);
    }

    [Fact] // D9-11: a FROZEN/ancient HTF feed (newest candle far older than 10× its period) is
    // treated as unavailable → fail-safe Neutral, even while the LTF tick is fresh.
    public async Task Live_refresh_treats_frozen_htf_as_unavailable()
    {
        var frozenHtf = Series(TimeFrame.H4, up: true, end: Now.AddYears(-2)); // 2024 D-list, live now 2026
        var (coord, _) = BuildLive(TimeFrame.H1,
            ltfCandles: Series(TimeFrame.H1, up: true), htfCandles: frozenHtf);

        var r = await coord.RefreshAsync(hasOpenPosition: false);

        Assert.Equal(SignalDirection.Neutral, r.Analysis!.Signal.Direction);
        Assert.Equal(SignalClassifier.ReasonHtf, r.Analysis.Signal.PrimaryReason);
        Assert.False(r.Htf!.Available);
        Assert.Equal(FreshnessStatus.Fresh, r.Freshness.Status); // LTF freshness intact
    }

    [Fact] // IC-8: the HTF pull never feeds DataFreshnessMonitor — with NO tick, freshness is
    // driven by the LTF candle fallback (recent), NOT by the ancient HTF series.
    public async Task Htf_pull_does_not_affect_ltf_freshness()
    {
        var ancientHtf = Series(TimeFrame.H4, up: true, end: Now.AddYears(-2));
        var (coord, _) = BuildLive(TimeFrame.H1,
            ltfCandles: Series(TimeFrame.H1, up: true), htfCandles: ancientHtf, noTick: true);

        var r = await coord.RefreshAsync(hasOpenPosition: false);

        // If the ancient HTF series had leaked into freshness, this would be Stale.
        Assert.Equal(FreshnessStatus.Fresh, r.Freshness.Status);
    }

    [Fact] // IC-4 (coordinator): at the top of the ladder (D1) the coordinator marks HTF
    // not-applicable and pulls NO higher-timeframe series → the signal can fire.
    public async Task Live_refresh_at_top_of_ladder_marks_htf_not_applicable()
    {
        var (coord, provider) = BuildLive(TimeFrame.D1, ltfCandles: Series(TimeFrame.D1, up: true));

        var r = await coord.RefreshAsync(hasOpenPosition: false);

        Assert.False(r.Htf!.Applicable);
        Assert.Equal(new[] { TimeFrame.D1 }, provider.RequestedTimeFrames); // no HTF pull at all
        Assert.Equal(SignalDirection.Buy, r.Analysis!.Signal.Direction);    // fires (guard is a no-op)
    }

    [Fact] // AC-42.5: the HTF pull uses StepUp of the timeframe SNAPSHOTTED at the refresh start —
    // a mid-poll SetTimeFrame does not change this poll's HTF read. Ordered sequence proves it.
    public async Task Htf_uses_timeframe_snapshotted_at_start()
    {
        var gate = new TaskCompletionSource();
        var (coord, provider) = BuildLive(TimeFrame.H1,
            ltfCandles: Series(TimeFrame.H1, up: true), htfCandles: Series(TimeFrame.H4, up: true));
        provider.Gate = gate;

        // Everything up to the first incomplete await runs synchronously, so the LTF pull (H1)
        // is recorded before the task is returned; it then blocks on the gate.
        var task = coord.RefreshAsync(hasOpenPosition: false);
        Assert.Equal(new[] { TimeFrame.H1 }, provider.RequestedTimeFrames);

        coord.SetTimeFrame(TimeFrame.D1); // switch WHILE the poll is in flight
        gate.SetResult();
        await task;

        // The in-flight poll pulled entirely off the H1 snapshot: LTF H1 then HTF H4 (=StepUp(H1)),
        // NEVER D1 or StepUp(D1). Proves the HTF read cannot be mislabeled by a mid-poll switch.
        Assert.Equal(new[] { TimeFrame.H1, TimeFrame.H4 }, provider.RequestedTimeFrames);
        Assert.Equal(TimeFrame.D1, coord.CurrentTimeFrame);
    }

    // ===================== FR-43: audit provenance =====================

    [Fact] // AC-43.1: the audit entry records the three distinguishable HTF states, never one
    // ambiguous null: confirmed (H4/Buy), not-applicable at D1 (none/n-a), unavailable (H4/unavailable).
    public async Task Audit_entry_records_htf_provenance()
    {
        // confirmed
        var (c1, _) = BuildLive(TimeFrame.H1, Series(TimeFrame.H1, up: true), Series(TimeFrame.H4, up: true));
        var confirmed = LiveSignalAuditEntry.From(await c1.RefreshAsync(false), NormalizedSymbol.Gold, TimeFrame.H1, Now);
        Assert.Equal("H4", confirmed.HtfTimeFrame);
        Assert.Equal("Buy", confirmed.HtfDirection);

        // not applicable (top of ladder)
        var (c2, _) = BuildLive(TimeFrame.D1, Series(TimeFrame.D1, up: true));
        var na = LiveSignalAuditEntry.From(await c2.RefreshAsync(false), NormalizedSymbol.Gold, TimeFrame.D1, Now);
        Assert.Equal("none", na.HtfTimeFrame);
        Assert.Equal("n/a", na.HtfDirection);

        // unavailable (HTF pull failed)
        var (c3, _) = BuildLive(TimeFrame.H1, Series(TimeFrame.H1, up: true), throwOnHtf: TimeFrame.H4);
        var unavail = LiveSignalAuditEntry.From(await c3.RefreshAsync(false), NormalizedSymbol.Gold, TimeFrame.H1, Now);
        Assert.Equal("H4", unavail.HtfTimeFrame);
        Assert.Equal("unavailable", unavail.HtfDirection);
    }

    [Fact] // AC-43.1: a gate-SUPPRESSED refresh (never reached the HTF read) records NO HTF fields
    // — INV-4: no fabricated HTF provenance when there was no HTF read.
    public async Task Audit_entry_has_no_htf_fields_when_suppressed()
    {
        // Never connected → suppressed before any candle/HTF pull.
        var clock = new ManualClock(Now);
        var provider = new TestMarketDataProvider()
            .WithSymbols(new BrokerSymbol("XAUUSD", "Gold"))
            .WithCandles(TimeFrame.H1, Series(TimeFrame.H1, up: true));
        var options = new LiveSignalOptions(NormalizedSymbol.Gold, TimeFrame.H1, 80, SymbolSpec.Gold(),
            10_000m, new SignalContext());
        var coord = new LiveSignalCoordinator(provider, new SignalAnalysisService(clock), new SignalGate(),
            new DataFreshnessMonitor(clock, TimeSpan.FromMinutes(90)), options, hasSymbolMapping: true, clock);

        var r = await coord.RefreshAsync(false);
        Assert.False(r.SignalAllowed);
        Assert.Null(r.Htf);

        var entry = LiveSignalAuditEntry.From(r, NormalizedSymbol.Gold, TimeFrame.H1, Now);
        Assert.Null(entry.HtfTimeFrame);
        Assert.Null(entry.HtfDirection);
    }
}
