using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Testing;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// FR-10: the test provider behaves as a normal IMarketDataProvider in tests.
public class TestProviderTests
{
    [Fact]
    public async Task Serves_scripted_symbols_and_tick_and_is_not_live()
    {
        var tick = new MarketTick(NormalizedSymbol.Gold, 2400m, 2400.2m, DateTimeOffset.UtcNow);
        var provider = new TestMarketDataProvider()
            .WithSymbols(new BrokerSymbol("XAUUSD"), new BrokerSymbol("EURUSD"))
            .WithLatestTick(tick);

        Assert.False(provider.IsLive);
        await provider.ConnectAsync();

        var symbols = await provider.GetAvailableSymbolsAsync();
        Assert.Equal(2, symbols.Count);

        var latest = await provider.GetLatestTickAsync(NormalizedSymbol.Gold);
        Assert.Equal(2400.1m, latest!.Mid);
    }
}
