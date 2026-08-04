using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Presentation;

/// <summary>
/// Cycle 8 (FR-39): builds the deterministic DEMO candle series for the non-live
/// sample path, at a caller-chosen <see cref="TimeFrame"/>. Extracted from
/// <c>App.BuildSampleCandles</c> so the timeframe-aware generation is unit-testable
/// headlessly (the WPF composition root is not).
///
/// This is explicitly NOT live data (INV-4): it fabricates no "live" price and claims
/// none — the analytical core treats it as Csv/Test grade. The candles carry the
/// selected timeframe both as their <see cref="Candle.TimeFrame"/> label AND as their
/// timestamp spacing (minutes-per-candle = the enum value), so a series built for D1 is
/// day-spaced and a series built for M1 is minute-spaced — honest at every timeframe.
/// </summary>
public static class SampleCandleSeries
{
    /// <summary>Deterministic rising series of <paramref name="count"/> candles at
    /// <paramref name="timeFrame"/>. Same shape as the prior hardcoded H1 demo series,
    /// now parameterised by timeframe.</summary>
    public static IReadOnlyList<Candle> Build(TimeFrame timeFrame, int count = 80)
    {
        if (count <= 0) return Array.Empty<Candle>();

        var list = new List<Candle>(count);
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        int minutesPerCandle = (int)timeFrame; // TimeFrame enum values ARE minutes-per-candle
        decimal basePrice = 1900m;
        for (int i = 0; i < count; i++)
        {
            decimal open = basePrice + i * 1.5m;
            decimal close = open + 1.0m;
            decimal high = close + 0.5m;
            decimal low = open - 0.5m;
            list.Add(new Candle(
                NormalizedSymbol.Gold, timeFrame,
                start.AddMinutes(minutesPerCandle * (double)i),
                open, high, low, close, volume: 1000 + i));
        }
        return list;
    }
}
