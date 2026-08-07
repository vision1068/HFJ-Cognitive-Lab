using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Analysis;
using GoldSignalAnalyzer.Application.Freshness;
using GoldSignalAnalyzer.Application.Live;
using GoldSignalAnalyzer.Application.Scoring;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Presentation;
using GoldSignalAnalyzer.Testing;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// Cycle 8 (FR-39 / FR-40) — the runtime timeframe selector and live-feed timeframe
/// switch. Load-bearing properties:
///   * the selector offers the curated set, defaults to H1, and raises Changed only on a
///     real change (FR-39);
///   * the sample series is rebuilt at the selected timeframe's spacing/label, and the
///     analytical core runs for every curated timeframe (FR-39);
///   * switching the live timeframe re-pulls candles at the new timeframe through the
///     SAME veto gate (never bypassing suppression), and an in-flight poll keeps the
///     timeframe it started with — no mid-poll mislabel (FR-40 / INV-4);
///   * the live strip shows a paused "recalculating" banner during the switch gap (FR-40).
/// </summary>
public class TimeFrameSelectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    // ---- FR-39: TimeFrameSelectionViewModel -------------------------------------

    [Fact] // AC-39.1
    public void Selector_offers_curated_set_default_h1()
    {
        var vm = new TimeFrameSelectionViewModel();
        Assert.Equal(TimeFrame.H1, vm.Selected);
        Assert.Equal(
            new[] { TimeFrame.M1, TimeFrame.M5, TimeFrame.M15, TimeFrame.M30, TimeFrame.H1, TimeFrame.H4, TimeFrame.D1 },
            vm.Choices.Select(c => c.Value).ToArray());
        // Labels are the enum names (never a percentage/probability — INV-5).
        Assert.Equal("H1", vm.Choices.Single(c => c.Value == TimeFrame.H1).Label);
        Assert.Equal("D1", vm.Choices.Single(c => c.Value == TimeFrame.D1).Label);
    }

    [Fact] // AC-39.2
    public void Selecting_new_timeframe_raises_changed_once()
    {
        var vm = new TimeFrameSelectionViewModel(TimeFrame.H1);
        var fired = new List<TimeFrame>();
        vm.Changed += (_, tf) => fired.Add(tf);

        vm.Selected = TimeFrame.D1;

        Assert.Single(fired);
        Assert.Equal(TimeFrame.D1, fired[0]);
        Assert.Equal(TimeFrame.D1, vm.Selected);
    }

    [Fact] // AC-39.2
    public void Selecting_same_timeframe_is_noop()
    {
        var vm = new TimeFrameSelectionViewModel(TimeFrame.H1);
        int count = 0;
        vm.Changed += (_, _) => count++;

        vm.Selected = TimeFrame.H1; // same as default

        Assert.Equal(0, count);
    }

    [Theory] // AC-39.3
    [InlineData(TimeFrame.M1, 1)]
    [InlineData(TimeFrame.H1, 60)]
    [InlineData(TimeFrame.D1, 1440)]
    public void Sample_series_uses_selected_timeframe_spacing_and_label(TimeFrame tf, int expectedMinutes)
    {
        var candles = SampleCandleSeries.Build(tf, count: 80);

        Assert.Equal(80, candles.Count);
        Assert.All(candles, c => Assert.Equal(tf, c.TimeFrame));
        var spacing = candles[1].OpenTimeUtc - candles[0].OpenTimeUtc;
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), spacing);
    }

    [Fact] // AC-39.4: the analytical core runs for EVERY curated timeframe (no throw, real result).
    public void Analyze_runs_for_every_curated_timeframe()
    {
        var svc = new SignalAnalysisService(new ManualClock(Now));
        foreach (var tf in TimeFrameSelectionViewModel.Curated)
        {
            var candles = SampleCandleSeries.Build(tf);
            var a = svc.Analyze(NormalizedSymbol.Gold, tf, candles, SymbolSpec.Gold(),
                10_000m, new SignalContext(HtfDirection: SignalDirection.Buy), hasOpenPosition: false);
            Assert.NotNull(a);
            Assert.NotEmpty(a.ExplanationLines);
        }
    }

    [Fact] // AC-39.5: the selector is passive — no order/command affordance (INV-1).
    public void Selector_exposes_no_order_member()
    {
        var members = typeof(TimeFrameSelectionViewModel).GetMembers()
            .Select(m => m.Name.ToLowerInvariant()).ToList();
        foreach (var forbidden in new[] { "command", "open", "close", "buy", "sell", "execute", "submit", "order", "trade", "place" })
            Assert.DoesNotContain(members, m => m.Contains(forbidden));
    }

    // ---- FR-40: live timeframe switch -------------------------------------------

    private static IReadOnlyList<Candle> RisingCandles(int n = 80)
    {
        var list = new List<Candle>();
        var start = Now.AddHours(-(n - 1));
        for (int i = 0; i < n; i++)
        {
            decimal open = 1900m + i * 1.5m;
            list.Add(new Candle(NormalizedSymbol.Gold, TimeFrame.H1, start.AddHours(i),
                open, open + 1.5m, open - 0.5m, open + 1.0m, volume: 1000 + i));
        }
        return list;
    }

    private static LiveSignalCoordinator BuildCoord(
        IMarketDataProvider provider, DateTimeOffset now, TimeSpan? staleAfter = null)
    {
        var clock = new ManualClock(now);
        return new LiveSignalCoordinator(
            provider, new SignalAnalysisService(clock), new SignalGate(),
            new DataFreshnessMonitor(clock, staleAfter ?? TimeSpan.FromMinutes(90)),
            new LiveSignalOptions(NormalizedSymbol.Gold, TimeFrame.H1, 80, SymbolSpec.Gold(),
                10_000m, new SignalContext(HtfDirection: SignalDirection.Buy)),
            hasSymbolMapping: true, clock);
    }

    [Fact] // AC-40.1: SetTimeFrame changes CurrentTimeFrame and the next pull uses it.
    public async Task SetTimeFrame_repulls_candles_at_new_timeframe()
    {
        var provider = new TestMarketDataProvider()
            .WithSymbols(new BrokerSymbol("XAUUSD", "Gold"))
            .WithCandles(RisingCandles())
            .WithLatestTick(new MarketTick(NormalizedSymbol.Gold, 1990m, 1990.2m, Now));
        await provider.ConnectAsync();
        var coord = BuildCoord(provider, Now);

        coord.SetTimeFrame(TimeFrame.D1);
        Assert.Equal(TimeFrame.D1, coord.CurrentTimeFrame);

        var r = await coord.RefreshAsync(hasOpenPosition: false);

        Assert.True(r.SignalAllowed);                          // still analyses at the new TF
        Assert.Equal(TimeFrame.D1, provider.LastRequestedTimeFrame); // re-pulled at D1
    }

    [Fact] // AC-40.2: a switch does NOT bypass the veto gate — stale data still suppresses.
    public async Task SetTimeFrame_does_not_bypass_stale_suppression()
    {
        var provider = new TestMarketDataProvider()
            .WithSymbols(new BrokerSymbol("XAUUSD", "Gold"))
            .WithCandles(RisingCandles())
            .WithLatestTick(new MarketTick(NormalizedSymbol.Gold, 1990m, 1990.2m, Now.AddHours(-3))); // 3h old
        await provider.ConnectAsync();
        var coord = BuildCoord(provider, Now, staleAfter: TimeSpan.FromMinutes(90));

        coord.SetTimeFrame(TimeFrame.M1);
        var r = await coord.RefreshAsync(hasOpenPosition: false);

        Assert.False(r.SignalAllowed);
        Assert.Null(r.Analysis);
        Assert.Equal(SignalGate.StaleReason, r.SuppressionReason);
        Assert.Equal(TimeFrame.M1, provider.LastRequestedTimeFrame); // it DID re-pull at M1, then vetoed
    }

    [Fact] // AC-40.3: an in-flight refresh keeps the timeframe it started with (snapshot).
    public async Task Refresh_uses_timeframe_snapshotted_at_start()
    {
        var release = new TaskCompletionSource();
        var provider = new GatedCandleProvider(RisingCandles(),
            new MarketTick(NormalizedSymbol.Gold, 1990m, 1990.2m, Now), release.Task);
        await provider.ConnectAsync();
        var coord = BuildCoord(provider, Now);

        // Start a refresh at H1; it will block inside GetCandlesAsync until we release it.
        var refreshTask = coord.RefreshAsync(hasOpenPosition: false);
        await provider.CandlesRequested;          // ensure the pull (and thus the snapshot) happened

        // Switch timeframe WHILE the poll is in flight.
        coord.SetTimeFrame(TimeFrame.D1);
        release.SetResult();
        await refreshTask;

        // The in-flight poll must have pulled entirely off the ORIGINAL H1 snapshot, not D1:
        // the LTF pull at H1 and the Cycle-9 HTF pull at H4 (= StepUp(H1)), in that order —
        // NEVER D1 or StepUp(D1). This proves the HTF read rides the same start-of-refresh
        // snapshot (D9-7), so a mid-poll switch cannot leak a stale HTF read into this poll.
        Assert.Equal(new[] { TimeFrame.H1, TimeFrame.H4 }, provider.RequestedTimeFrames);
        // But the coordinator now reports the newly-selected timeframe for the NEXT poll.
        Assert.Equal(TimeFrame.D1, coord.CurrentTimeFrame);
    }

    [Fact] // AC-40.4: the live strip shows a paused "recalculating" banner during a switch.
    public void MarkRecalculating_shows_paused_recalc_banner()
    {
        var vm = new LiveStatusViewModel();
        vm.MarkRecalculating(TimeFrame.D1);

        Assert.True(vm.IsLive);
        Assert.True(vm.IsSuppressed); // paused → never reads as an actionable call (INV-4)
        Assert.Contains("Recalculating", vm.Banner);
        Assert.Contains("D1", vm.Banner);
    }

    /// <summary>
    /// A minimal live-ish provider whose candle pull BLOCKS on a supplied task, so a test
    /// can switch the timeframe while a refresh is mid-flight and prove the snapshot.
    /// (Test-only; lives in the test assembly.)
    /// </summary>
    private sealed class GatedCandleProvider : IMarketDataProvider
    {
        private readonly IReadOnlyList<Candle> _candles;
        private readonly MarketTick _tick;
        private readonly Task _release;
        private readonly TaskCompletionSource _requested = new();

        public GatedCandleProvider(IReadOnlyList<Candle> candles, MarketTick tick, Task release)
        {
            _candles = candles;
            _tick = tick;
            _release = release;
        }

        public string Name => "Gated";
        public bool IsLive => false;
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public TimeFrame? LastRequestedTimeFrame { get; private set; }

        private readonly List<TimeFrame> _requestedTimeFrames = new();
        /// <summary>Cycle 9: the ordered sequence of requested timeframes across one refresh
        /// (LTF then HTF), so the snapshot proof can assert the whole poll used the snapshot.</summary>
        public IReadOnlyList<TimeFrame> RequestedTimeFrames => _requestedTimeFrames;

        /// <summary>Completes once GetCandlesAsync has been entered (and the snapshot taken).</summary>
        public Task CandlesRequested => _requested.Task;

        public Task<ConnectionState> ConnectAsync(CancellationToken ct = default)
        {
            State = ConnectionState.Connected;
            return Task.FromResult(State);
        }

        public Task DisconnectAsync(CancellationToken ct = default)
        {
            State = ConnectionState.Disconnected;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BrokerSymbol>> GetAvailableSymbolsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BrokerSymbol>>(new[] { new BrokerSymbol("XAUUSD", "Gold") });

        public Task<MarketTick?> GetLatestTickAsync(NormalizedSymbol symbol, CancellationToken ct = default)
            => Task.FromResult<MarketTick?>(_tick);

        public async Task<IReadOnlyList<Candle>> GetCandlesAsync(
            NormalizedSymbol symbol, TimeFrame timeFrame, int count, CancellationToken ct = default)
        {
            LastRequestedTimeFrame = timeFrame; // recorded at the moment of the call (post-snapshot)
            _requestedTimeFrames.Add(timeFrame);
            _requested.TrySetResult();
            await _release.ConfigureAwait(false);
            return _candles.TakeLast(count).ToList();
        }
    }
}
