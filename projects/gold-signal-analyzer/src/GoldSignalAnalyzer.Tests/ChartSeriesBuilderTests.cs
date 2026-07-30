using GoldSignalAnalyzer.Application.Charting;
using GoldSignalAnalyzer.Domain;
using IndicatorMath = GoldSignalAnalyzer.Application.Indicators.Indicators;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// Cycle 4 (FR-27, AC-27.1/27.4) — the pure Application chart-data builder. It projects
/// existing candles + reuses the already-tested EMA calculator; it fabricates nothing.
/// </summary>
public class ChartSeriesBuilderTests
{
    private static List<Candle> Series(int n, bool rising = true)
    {
        var list = new List<Candle>();
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (int i = 0; i < n; i++)
        {
            decimal open = rising ? 1900m + i * 1.5m : 1900m - i * 1.5m;
            decimal close = open + (rising ? 1.0m : -1.0m);
            decimal high = Math.Max(open, close) + 0.5m;
            decimal low = Math.Min(open, close) - 0.5m;
            list.Add(new Candle(NormalizedSymbol.Gold, TimeFrame.H1, start.AddHours(i),
                open, high, low, close, 1000 + i));
        }
        return list;
    }

    [Fact] // AC-27.1: bars map 1:1 and price extent spans the real high/low
    public void Build_maps_bars_and_price_extent()
    {
        var candles = Series(40);
        var data = new ChartSeriesBuilder().Build(candles);

        Assert.Equal(40, data.Bars.Count);
        Assert.Equal(candles.Min(c => c.Low), data.Min);
        Assert.Equal(candles.Max(c => c.High), data.Max);
        Assert.False(data.IsEmpty);
    }

    [Fact] // AC-27.1: overlays are EMA series aligned index-for-index, null during warmup
    public void Build_overlays_are_aligned_ema_series_with_warmup_nulls()
    {
        var candles = Series(40);
        var data = new ChartSeriesBuilder(new ChartConfig { EmaFastPeriod = 12, EmaSlowPeriod = 26 }).Build(candles);

        Assert.Equal(2, data.Overlays.Count);
        var fast = data.Overlays[0];
        Assert.Equal("EMA12", fast.Name);
        Assert.Equal(40, fast.Values.Count);                       // aligned to bars
        Assert.Null(fast.Values[10]);                              // warmup (period 12) ⇒ null
        Assert.NotNull(fast.Values[^1]);                           // populated at the end

        // The last overlay value equals the independently-computed EMA of the closes.
        var closes = candles.Select(c => c.Close).ToList();
        Assert.Equal(IndicatorMath.Ema(closes, 12), fast.Values[^1]!.Value, 6);
    }

    [Fact] // AC-27.4: an empty or one-candle series fabricates no bars
    public void Build_empty_or_single_yields_no_fabricated_bars()
    {
        Assert.True(new ChartSeriesBuilder().Build(null).IsEmpty);
        Assert.True(new ChartSeriesBuilder().Build(new List<Candle>()).IsEmpty);

        var one = new ChartSeriesBuilder().Build(Series(1));
        Assert.Single(one.Bars);                                   // exactly the one real bar, no invented neighbours
        Assert.All(one.Overlays, o => Assert.All(o.Values, v => Assert.Null(v))); // too short for any EMA ⇒ all null
    }

    [Fact] // MaxBars keeps only the most-recent window, in chronological order
    public void Build_respects_max_bars_window()
    {
        var candles = Series(200);
        var data = new ChartSeriesBuilder(new ChartConfig { MaxBars = 50 }).Build(candles);

        Assert.Equal(50, data.Bars.Count);
        Assert.Equal(candles[^1].OpenTimeUtc, data.Bars[^1].OpenTimeUtc);   // newest bar preserved
        Assert.Equal(candles[^50].OpenTimeUtc, data.Bars[0].OpenTimeUtc);   // window is the last 50
    }

    [Fact] // AC-27.3: up vs down bars are distinguishable
    public void Bars_flag_up_and_down_direction()
    {
        Assert.True(new ChartSeriesBuilder().Build(Series(30, rising: true)).Bars[^1].IsUp);
        Assert.False(new ChartSeriesBuilder().Build(Series(30, rising: false)).Bars[^1].IsUp);
    }
}
