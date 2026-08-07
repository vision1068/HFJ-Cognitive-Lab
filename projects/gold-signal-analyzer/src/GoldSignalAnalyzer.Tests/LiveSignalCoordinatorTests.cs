using GoldSignalAnalyzer.Application.Analysis;
using GoldSignalAnalyzer.Application.Freshness;
using GoldSignalAnalyzer.Application.Live;
using GoldSignalAnalyzer.Application.News;
using GoldSignalAnalyzer.Application.Scoring;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Testing;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// Cycle 7 (FR-36/FR-37) — the live coordinator that pulls from a LIVE provider,
/// tracks freshness, and runs the FR-12 veto gate before analysing. Proven against
/// <see cref="TestMarketDataProvider"/> + <see cref="ManualClock"/> — no real
/// terminal. The load-bearing property: on ANY unsafe condition (not connected,
/// no mapping, stale/future/never-seen data, empty pull) the result is SUPPRESSED
/// with the exact banner and a NULL analysis — never a fabricated price (INV-4/NFR-5).
/// </summary>
public class LiveSignalCoordinatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);

    private static IReadOnlyList<Candle> RisingCandles(DateTimeOffset lastOpen, int n = 80)
    {
        var list = new List<Candle>();
        var start = lastOpen.AddHours(-(n - 1));
        decimal basePrice = 1900m;
        for (int i = 0; i < n; i++)
        {
            decimal open = basePrice + i * 1.5m;
            decimal close = open + 1.0m;
            decimal high = close + 0.5m;
            decimal low = open - 0.5m;
            list.Add(new Candle(NormalizedSymbol.Gold, TimeFrame.H1, start.AddHours(i),
                open, high, low, close, volume: 1000 + i));
        }
        return list;
    }

    private static LiveSignalOptions Options() => new(
        NormalizedSymbol.Gold, TimeFrame.H1, CandleCount: 80,
        SymbolSpec.Gold(), AccountBalance: 10_000m,
        Context: new SignalContext(HtfDirection: SignalDirection.Buy));

    private static (LiveSignalCoordinator coord, ManualClock clock) Build(
        TestMarketDataProvider provider, bool hasMapping = true, TimeSpan? staleAfter = null)
    {
        var clock = new ManualClock(Now);
        var analysis = new SignalAnalysisService(clock);
        var freshness = new DataFreshnessMonitor(clock, staleAfter ?? TimeSpan.FromMinutes(90));
        var coord = new LiveSignalCoordinator(
            provider, analysis, new SignalGate(), freshness, Options(), hasMapping, clock);
        return (coord, clock);
    }

    private static TestMarketDataProvider ConnectedProvider(DateTimeOffset? tickTime, IReadOnlyList<Candle>? candles = null)
    {
        var p = new TestMarketDataProvider()
            .WithSymbols(new BrokerSymbol("XAUUSD", "Gold"))
            .WithCandles(candles ?? RisingCandles(Now));
        if (tickTime is not null)
            p.WithLatestTick(new MarketTick(NormalizedSymbol.Gold, 1990m, 1990.2m, tickTime.Value));
        p.ConnectAsync().GetAwaiter().GetResult();
        return p;
    }

    [Fact] // Not connected → suppressed (CONNECTION NOT READY), no analysis, no data pulled.
    public async Task Disconnected_suppresses_with_not_connected_reason()
    {
        var provider = new TestMarketDataProvider().WithCandles(RisingCandles(Now)); // never connected
        var (coord, _) = Build(provider);

        var r = await coord.RefreshAsync(hasOpenPosition: false);

        Assert.False(r.SignalAllowed);
        Assert.Null(r.Analysis);
        Assert.Equal(SignalGate.NotConnectedReason, r.SuppressionReason);
        Assert.Equal(ConnectionState.Disconnected, r.ConnectionState);
    }

    [Fact] // Connected but no confirmed gold mapping → suppressed (NO GOLD SYMBOL MAPPED).
    public async Task No_symbol_mapping_suppresses()
    {
        var provider = ConnectedProvider(tickTime: Now);
        var (coord, _) = Build(provider, hasMapping: false);

        var r = await coord.RefreshAsync(hasOpenPosition: false);

        Assert.False(r.SignalAllowed);
        Assert.Null(r.Analysis);
        Assert.Equal(SignalGate.NoSymbolReason, r.SuppressionReason);
    }

    [Fact] // Connected + mapped + a fresh tick → gate ALLOWS, analysis is produced from real candles.
    public async Task Connected_fresh_mapped_allows_signal()
    {
        var provider = ConnectedProvider(tickTime: Now);
        var (coord, _) = Build(provider);

        var r = await coord.RefreshAsync(hasOpenPosition: false);

        Assert.True(r.SignalAllowed);
        Assert.NotNull(r.Analysis);
        Assert.Null(r.SuppressionReason);
        Assert.Equal(FreshnessStatus.Fresh, r.Freshness.Status);
        Assert.Equal(80, r.Candles.Count);
    }

    [Fact] // Connected but the last tick is older than the stale threshold → suppressed (DATA STALE).
    public async Task Stale_data_suppresses_with_stale_reason()
    {
        var provider = ConnectedProvider(tickTime: Now.AddHours(-3)); // 3h old, threshold 90m
        var (coord, _) = Build(provider);

        var r = await coord.RefreshAsync(hasOpenPosition: false);

        Assert.False(r.SignalAllowed);
        Assert.Null(r.Analysis);
        Assert.Equal(SignalGate.StaleReason, r.SuppressionReason);
        Assert.Equal(FreshnessStatus.Stale, r.Freshness.Status);
    }

    [Fact] // NFR-5: a future-dated tick is never trusted as live → suppressed (DATA STALE).
    public async Task Future_dated_tick_suppresses()
    {
        var provider = ConnectedProvider(tickTime: Now.AddMinutes(10)); // beyond skew tolerance
        var (coord, _) = Build(provider);

        var r = await coord.RefreshAsync(hasOpenPosition: false);

        Assert.False(r.SignalAllowed);
        Assert.Equal(SignalGate.StaleReason, r.SuppressionReason);
    }

    [Fact] // Cycle 10 (FR-44): a live refresh during a High-impact blackout window is forced Neutral,
           // even though the connection/freshness gate itself allows the refresh.
    public async Task News_blackout_suppresses_even_when_otherwise_allowed()
    {
        var provider = ConnectedProvider(tickTime: Now);
        var clock = new ManualClock(Now);
        var analysis = new SignalAnalysisService(clock);
        var freshness = new DataFreshnessMonitor(clock, TimeSpan.FromMinutes(90));
        var calendar = new StaticEconomicCalendarProvider(new[]
        {
            new EconomicEvent(Now, "FOMC Rate Decision", EventImpact.High),
        });
        var newsGate = new NewsBlackoutGate(calendar, TimeSpan.FromMinutes(30));
        var coord = new LiveSignalCoordinator(
            provider, analysis, new SignalGate(), freshness, Options(), hasSymbolMapping: true, clock,
            newsGate: newsGate);

        var r = await coord.RefreshAsync(hasOpenPosition: false);

        Assert.True(r.Decision.IsAllowed);      // the connection/freshness gate DID allow the refresh
        Assert.NotNull(r.Analysis);             // analysis still runs — this is a classifier veto, not a gate suppression
        Assert.Equal(SignalDirection.Neutral, r.Analysis!.Signal.Direction);
        Assert.Equal(SignalClassifier.ReasonNews, r.Analysis.Signal.PrimaryReason);
    }

    [Fact] // Backward compatibility: no news gate configured (default null) → identical to pre-Cycle-10 behaviour.
    public async Task No_news_gate_configured_preserves_prior_behaviour()
    {
        var provider = ConnectedProvider(tickTime: Now);
        var (coord, _) = Build(provider); // newsGate defaults to null
        var r = await coord.RefreshAsync(hasOpenPosition: false);
        Assert.True(r.SignalAllowed);
    }

    [Fact] // Never-seen data (no tick, no candles) → suppressed, nothing fabricated.
    public async Task Empty_pull_suppresses_and_does_not_fabricate()
    {
        var provider = ConnectedProvider(tickTime: null, candles: Array.Empty<Candle>());
        var (coord, _) = Build(provider);

        var r = await coord.RefreshAsync(hasOpenPosition: false);

        Assert.False(r.SignalAllowed);
        Assert.Null(r.Analysis);
        Assert.Empty(r.Candles);
        Assert.NotNull(r.SuppressionReason);
    }

    [Fact] // INV-1: the coordinator and its result expose NO order/execution surface.
    public void Coordinator_exposes_no_order_or_execution_member()
    {
        foreach (var t in new[] { typeof(LiveSignalCoordinator), typeof(LiveRefreshResult) })
        {
            var members = t.GetMembers().Select(m => m.Name.ToLowerInvariant()).ToList();
            foreach (var forbidden in new[] { "open", "close", "buy", "sell", "execute", "submit", "order", "trade", "place" })
                Assert.DoesNotContain(members, m => m.Contains(forbidden));
        }
    }
}
