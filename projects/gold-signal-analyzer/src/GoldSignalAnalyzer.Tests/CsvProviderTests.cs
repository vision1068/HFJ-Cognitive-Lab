using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Infrastructure.Providers;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// FR-10: CSV historical provider (backtest/replay), FR-11: not live.
public class CsvProviderTests
{
    private static readonly string[] Csv =
    {
        "time,open,high,low,close,volume",
        "2026-07-30T09:00:00Z,2400,2410,2395,2405,100",
        "2026-07-30T10:00:00Z,2405,2420,2402,2418,150",
        "2026-07-30T11:00:00Z,2418,2419,2400,2402,120",
    };

    [Fact]
    public void Parses_candles_and_is_not_live()
    {
        var p = CsvHistoricalMarketDataProvider.ParseCsv(NormalizedSymbol.Gold, TimeFrame.H1, Csv);
        Assert.False(p.IsLive);
    }

    [Fact]
    public async Task Returns_last_n_candles_in_order()
    {
        var p = CsvHistoricalMarketDataProvider.ParseCsv(NormalizedSymbol.Gold, TimeFrame.H1, Csv);
        await p.ConnectAsync();
        var candles = await p.GetCandlesAsync(NormalizedSymbol.Gold, TimeFrame.H1, 2);
        Assert.Equal(2, candles.Count);
        Assert.Equal(2405m, candles[0].Open);    // 10:00 bar
        Assert.Equal(2418m, candles[^1].Open);   // 11:00 bar
        Assert.Equal(2402m, candles[^1].Close);  // 11:00 bar close
    }

    [Fact]
    public async Task Latest_tick_reconstructs_last_close_as_zero_spread()
    {
        var p = CsvHistoricalMarketDataProvider.ParseCsv(NormalizedSymbol.Gold, TimeFrame.H1, Csv);
        await p.ConnectAsync();
        var tick = await p.GetLatestTickAsync(NormalizedSymbol.Gold);
        Assert.NotNull(tick);
        Assert.Equal(2402m, tick!.Bid);
        Assert.Equal(0m, tick.Spread);
    }

    [Fact]
    public async Task No_tick_before_connect()
    {
        var p = CsvHistoricalMarketDataProvider.ParseCsv(NormalizedSymbol.Gold, TimeFrame.H1, Csv);
        Assert.Null(await p.GetLatestTickAsync(NormalizedSymbol.Gold));
    }

    [Fact]
    public async Task LoadFromFile_reads_a_real_csv()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gsa-csv-{Guid.NewGuid():N}.csv");
        await File.WriteAllLinesAsync(path, Csv);
        try
        {
            var p = CsvHistoricalMarketDataProvider.LoadFromFile(NormalizedSymbol.Gold, TimeFrame.H1, path);
            await p.ConnectAsync();
            var candles = await p.GetCandlesAsync(NormalizedSymbol.Gold, TimeFrame.H1, 10);
            Assert.Equal(3, candles.Count);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Malformed_row_is_rejected()
        => Assert.Throws<FormatException>(() =>
            CsvHistoricalMarketDataProvider.ParseCsv(NormalizedSymbol.Gold, TimeFrame.H1,
                new[] { "time,open,high,low,close,volume", "2026-07-30T09:00:00Z,2400,2410" }));
}
