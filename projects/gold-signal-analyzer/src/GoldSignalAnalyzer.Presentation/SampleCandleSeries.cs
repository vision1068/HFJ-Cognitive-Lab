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
    /// <summary>
    /// Deterministic, noisy series of <paramref name="count"/> candles at
    /// <paramref name="timeFrame"/> — a seeded random walk with a gentle upward drift,
    /// not a straight ramp (2026-08-07: the original hardcoded-slope series made every
    /// chart a perfectly straight line and gave Fibonacci/liquidity-sweep evidence no real
    /// swings/wicks to ever detect). The seed is derived ONLY from <paramref name="timeFrame"/>
    /// (never the wall clock), so the same timeframe reproduces byte-identical output on every
    /// call (FR-24 determinism) while different timeframes get genuinely different-looking
    /// paths rather than the same shape rescaled.</summary>
    public static IReadOnlyList<Candle> Build(TimeFrame timeFrame, int count = 80)
    {
        if (count <= 0) return Array.Empty<Candle>();

        var list = new List<Candle>(count);
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        int minutesPerCandle = (int)timeFrame; // TimeFrame enum values ARE minutes-per-candle
        var rng = new Random(unchecked(1_000_003 + (int)timeFrame * 97));

        decimal price = 1900m;
        const decimal trend = 0.35m; // gentle net upward drift per candle, like the old ramp
        for (int i = 0; i < count; i++)
        {
            decimal open = price;
            decimal noise = (decimal)(rng.NextDouble() * 4.0 - 2.0); // [-2, +2)
            decimal close = Math.Round(open + trend + noise, 2);
            if (close <= 0m) close = open; // guard against a pathological walk into negative price

            decimal bodyHigh = Math.Max(open, close);
            decimal bodyLow = Math.Min(open, close);
            decimal upperWick = Math.Round((decimal)(rng.NextDouble() * 1.5), 2);
            decimal lowerWick = Math.Round((decimal)(rng.NextDouble() * 1.5), 2);
            decimal high = bodyHigh + upperWick;
            decimal low = Math.Max(0.01m, bodyLow - lowerWick);

            list.Add(new Candle(
                NormalizedSymbol.Gold, timeFrame,
                start.AddMinutes(minutesPerCandle * (double)i),
                open, high, low, close, volume: 1000 + i));

            price = close;
        }
        return list;
    }
}
