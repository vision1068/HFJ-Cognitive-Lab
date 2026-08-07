using GoldSignalAnalyzer.Application.Bridge;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Infrastructure.Providers;
using GoldSignalAnalyzer.Tests.Support;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// FR-10 (live seam), FR-11 (never fabricate live data), FR-9 (require mapping).
public class Mt5ProviderSeamTests
{
    private static SymbolMapping GoldMap => new(new BrokerSymbol("XAUUSD"), NormalizedSymbol.Gold, SymbolMappingSource.AutoDetected, 0.9);

    [Fact]
    public void Is_flagged_as_live()
        => Assert.True(new MetaTrader5MarketDataProvider(new FakeBridgeClient()).IsLive);

    [Fact]
    public async Task Refuses_to_return_data_when_not_connected()
    {
        var p = new MetaTrader5MarketDataProvider(new FakeBridgeClient());
        p.UseSymbolMapping(GoldMap);
        // Never connected -> must throw, never fabricate a price (FR-11 / NFR-5).
        await Assert.ThrowsAsync<InvalidOperationException>(() => p.GetLatestTickAsync(NormalizedSymbol.Gold));
    }

    [Fact]
    public async Task Healthy_bridge_with_terminal_connects()
    {
        var bridge = new FakeBridgeClient { Health = new BridgeHealth(true, true, DateTimeOffset.UtcNow) };
        var p = new MetaTrader5MarketDataProvider(bridge);
        var state = await p.ConnectAsync();
        Assert.Equal(ConnectionState.Connected, state);
    }

    [Fact]
    public async Task Bridge_up_but_terminal_detached_stays_reconnecting_not_connected()
    {
        var bridge = new FakeBridgeClient { Health = new BridgeHealth(true, false, DateTimeOffset.UtcNow) };
        var p = new MetaTrader5MarketDataProvider(bridge);
        var state = await p.ConnectAsync();
        Assert.Equal(ConnectionState.Reconnecting, state);
        p.UseSymbolMapping(GoldMap);
        await Assert.ThrowsAsync<InvalidOperationException>(() => p.GetLatestTickAsync(NormalizedSymbol.Gold));
    }

    [Fact]
    public async Task Unhealthy_bridge_faults()
    {
        var bridge = new FakeBridgeClient { Health = BridgeHealth.Unhealthy(DateTimeOffset.UtcNow, "down") };
        var p = new MetaTrader5MarketDataProvider(bridge);
        Assert.Equal(ConnectionState.Faulted, await p.ConnectAsync());
    }

    [Fact]
    public async Task Connected_but_unmapped_symbol_is_refused()
    {
        var bridge = new FakeBridgeClient();
        var p = new MetaTrader5MarketDataProvider(bridge);
        await p.ConnectAsync(); // healthy+terminal by default
        // No mapping set -> refuse (FR-9)
        await Assert.ThrowsAsync<InvalidOperationException>(() => p.GetLatestTickAsync(NormalizedSymbol.Gold));
    }

    [Fact]
    public async Task Connected_and_mapped_returns_bridge_tick()
    {
        var bridge = new FakeBridgeClient
        {
            Tick = new MarketTick(NormalizedSymbol.Gold, 2400.1m, 2400.3m, DateTimeOffset.UtcNow)
        };
        var p = new MetaTrader5MarketDataProvider(bridge);
        await p.ConnectAsync();
        p.UseSymbolMapping(GoldMap);
        var tick = await p.GetLatestTickAsync(NormalizedSymbol.Gold);
        Assert.NotNull(tick);
        Assert.Equal(2400.2m, tick!.Mid);
    }
}
