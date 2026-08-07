using GoldSignalAnalyzer.Domain;
using IndicatorMath = GoldSignalAnalyzer.Application.Indicators.Indicators;

namespace GoldSignalAnalyzer.Application.Charting;

/// <summary>FR-27 overlay configuration. Two EMA overlays by default; both draw from
/// the already-tested <see cref="Indicators.EmaSeries"/> so the chart adds NO new
/// indicator math to verify.</summary>
public sealed record ChartConfig
{
    public int EmaFastPeriod { get; init; } = 12;
    public int EmaSlowPeriod { get; init; } = 26;
    /// <summary>Show at most this many most-recent bars (0 = all). Keeps a snapshot chart readable.</summary>
    public int MaxBars { get; init; } = 120;
}

/// <summary>
/// FR-27 (AC-27.1): turns a <see cref="Candle"/> series into a <see cref="ChartData"/>
/// — OHLC bars plus EMA-fast/EMA-slow overlays aligned index-for-index. It is pure and
/// reuses the existing indicator calculators; it fabricates nothing. An empty (or
/// null) series yields <see cref="ChartData.Empty"/> — never an invented bar (INV-4,
/// AC-27.4).
/// </summary>
public sealed class ChartSeriesBuilder
{
    private readonly ChartConfig _cfg;

    public ChartSeriesBuilder(ChartConfig? cfg = null) => _cfg = cfg ?? new ChartConfig();

    public ChartData Build(IReadOnlyList<Candle>? candles)
    {
        if (candles is null || candles.Count == 0) return ChartData.Empty;

        // Take the most-recent window (a snapshot chart), preserving chronological order.
        IReadOnlyList<Candle> window = candles;
        if (_cfg.MaxBars > 0 && candles.Count > _cfg.MaxBars)
            window = candles.Skip(candles.Count - _cfg.MaxBars).ToList();

        var bars = new List<ChartBar>(window.Count);
        decimal min = decimal.MaxValue, max = decimal.MinValue;
        foreach (var c in window)
        {
            bars.Add(new ChartBar(c.OpenTimeUtc, c.Open, c.High, c.Low, c.Close));
            if (c.Low < min) min = c.Low;
            if (c.High > max) max = c.High;
        }

        var closes = window.Select(c => c.Close).ToList();
        var overlays = new List<ChartOverlaySeries>
        {
            BuildEma($"EMA{_cfg.EmaFastPeriod}", closes, _cfg.EmaFastPeriod),
            BuildEma($"EMA{_cfg.EmaSlowPeriod}", closes, _cfg.EmaSlowPeriod),
        };

        return new ChartData(bars, overlays, min, max);
    }

    private static ChartOverlaySeries BuildEma(string name, IReadOnlyList<decimal> closes, int period)
    {
        // EmaSeries is null-during-warmup and aligned to input; when the series is too
        // short it is all-null — the overlay simply draws nothing, never a placeholder.
        decimal?[] series = period > 0 && closes.Count > 0
            ? IndicatorMath.EmaSeries(closes, period)
            : new decimal?[closes.Count];
        return new ChartOverlaySeries(name, series);
    }
}
