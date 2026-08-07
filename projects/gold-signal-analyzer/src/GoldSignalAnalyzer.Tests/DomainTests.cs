using GoldSignalAnalyzer.Domain;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

public class DomainTests
{
    [Fact] // FR-9: canonical gold symbol is XAU/USD
    public void Gold_normalized_symbol_is_xau_usd()
        => Assert.Equal("XAU/USD", NormalizedSymbol.Gold.Value);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizedSymbol_rejects_blank(string v)
        => Assert.Throws<ArgumentException>(() => new NormalizedSymbol(v));

    [Fact]
    public void MarketTick_rejects_ask_below_bid()
        => Assert.Throws<ArgumentException>(() =>
            new MarketTick(NormalizedSymbol.Gold, 2400m, 2399m, DateTimeOffset.UtcNow));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MarketTick_rejects_nonpositive_prices(decimal bad)
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MarketTick(NormalizedSymbol.Gold, bad, bad, DateTimeOffset.UtcNow));

    [Fact]
    public void MarketTick_normalizes_timestamp_to_utc()
    {
        var tick = new MarketTick(NormalizedSymbol.Gold, 2400m, 2400.2m,
            new DateTimeOffset(2026, 7, 30, 10, 0, 0, TimeSpan.FromHours(3)));
        Assert.Equal(TimeSpan.Zero, tick.TimestampUtc.Offset);
        Assert.Equal(2400.1m, tick.Mid);
    }

    [Fact]
    public void Candle_rejects_high_below_low()
        => Assert.Throws<ArgumentException>(() =>
            new Candle(NormalizedSymbol.Gold, TimeFrame.M5, DateTimeOffset.UtcNow, 10, 9, 11, 10, 1));

    [Fact]
    public void Candle_accepts_valid_ohlc()
    {
        var c = new Candle(NormalizedSymbol.Gold, TimeFrame.H1, DateTimeOffset.UtcNow, 2400, 2410, 2395, 2405, 123);
        Assert.Equal(2410, c.High);
        Assert.Equal(TimeFrame.H1, c.TimeFrame);
    }
}
