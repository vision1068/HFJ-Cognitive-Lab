using GoldSignalAnalyzer.Application.Symbols;
using GoldSignalAnalyzer.Domain;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// FR-9: gold symbol detection across broker variants + manual mapping.
public class GoldSymbolResolverTests
{
    private readonly GoldSymbolResolver _resolver = new();

    [Theory]
    [InlineData("XAUUSD")]
    [InlineData("GOLD")]
    [InlineData("XAUUSDm")]
    [InlineData("XAUUSD.a")]
    [InlineData("XAUUSD.c")]
    [InlineData("XAUUSD.pro")]
    [InlineData("XAUUSD.raw")]
    [InlineData("XAUUSD.ecn")]
    [InlineData("GOLDmicro")]
    [InlineData("XAU/USD")]
    [InlineData("GOLD.spot")]
    public void Recognizes_all_known_gold_variants(string raw)
        => Assert.True(GoldSymbolResolver.ScoreGold(raw) > 0, $"'{raw}' should score as gold");

    [Theory]
    [InlineData("EURUSD")]
    [InlineData("XAGUSD")]   // silver, not gold
    [InlineData("XAGUSD.a")]
    [InlineData("XPTUSD")]   // platinum
    [InlineData("US30")]
    [InlineData("BTCUSD")]
    [InlineData("")]
    public void Rejects_non_gold_symbols(string raw)
        => Assert.Equal(0, GoldSymbolResolver.ScoreGold(raw));

    [Fact]
    public void Exact_XAUUSD_scores_highest()
    {
        Assert.Equal(1.0, GoldSymbolResolver.ScoreGold("XAUUSD"));
        Assert.True(GoldSymbolResolver.ScoreGold("XAUUSD") > GoldSymbolResolver.ScoreGold("XAUUSDm"));
        Assert.True(GoldSymbolResolver.ScoreGold("XAUUSDm") > GoldSymbolResolver.ScoreGold("GOLD"));
    }

    [Fact]
    public void Ranks_cleanest_symbol_first_among_many()
    {
        var symbols = new[] { "XAUUSD.pro", "XAUUSDm", "XAUUSD", "EURUSD", "GOLD", "XAGUSD" }
            .Select(s => new BrokerSymbol(s));
        var ranked = _resolver.RankGoldCandidates(symbols);

        Assert.Equal("XAUUSD", ranked[0].Broker.Raw);
        Assert.DoesNotContain(ranked, c => c.Broker.Raw == "EURUSD");
        Assert.DoesNotContain(ranked, c => c.Broker.Raw == "XAGUSD");
    }

    [Fact] // No hardcoded assumption of "XAUUSD": detection works when only GOLD exists.
    public void Detects_gold_when_only_GOLD_exists()
    {
        var mapping = _resolver.AutoDetect(new[] { new BrokerSymbol("GOLD"), new BrokerSymbol("EURUSD") });
        Assert.NotNull(mapping);
        Assert.Equal("GOLD", mapping!.Broker.Raw);
        Assert.Equal(NormalizedSymbol.Gold, mapping.Normalized);
        Assert.Equal(SymbolMappingSource.AutoDetected, mapping.Source);
    }

    [Fact]
    public void AutoDetect_returns_null_when_no_gold_present()
    {
        var mapping = _resolver.AutoDetect(new[] { new BrokerSymbol("EURUSD"), new BrokerSymbol("US30") });
        Assert.Null(mapping);
    }

    [Fact] // Manual override always maps to normalized gold with full confidence.
    public void Manual_selection_overrides_detector()
    {
        var mapping = _resolver.SelectManually(new BrokerSymbol("GOLD#"));
        Assert.Equal(SymbolMappingSource.ManualSelection, mapping.Source);
        Assert.Equal(1.0, mapping.Confidence);
        Assert.Equal(NormalizedSymbol.Gold, mapping.Normalized);
    }
}
