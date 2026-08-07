using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.MarketData;

/// <summary>
/// FR-11.4: aggregates lower-timeframe candles into a higher timeframe
/// (e.g. M1 → M5 → H1 → D1). The target must be a whole multiple of the source
/// (validated). A target bucket is <see cref="CandleStatus.Completed"/> only when
/// it holds the full expected number of source candles AND every source candle
/// is itself Completed; otherwise it is <see cref="CandleStatus.Provisional"/>
/// (FR-11.5 / FR-14 — confirmed signals use completed bars only).
/// </summary>
public static class CandleAggregator
{
    public static IReadOnlyList<Candle> Aggregate(
        NormalizedSymbol symbol,
        TimeFrame sourceTimeFrame,
        TimeFrame targetTimeFrame,
        IReadOnlyList<Candle> sourceCandles)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        if (sourceCandles is null) throw new ArgumentNullException(nameof(sourceCandles));
        int expected = TimeFrameBoundary.CandlesPerBucket(sourceTimeFrame, targetTimeFrame);

        var ordered = sourceCandles
            .Where(c => c.TimeFrame == sourceTimeFrame)
            .OrderBy(c => c.OpenTimeUtc)
            .ToList();

        var result = new List<Candle>();
        DateTimeOffset? bucketStart = null;
        decimal open = 0, high = 0, low = 0, close = 0, volume = 0;
        int count = 0;
        bool allCompleted = true;

        void Flush()
        {
            if (bucketStart is null) return;
            var status = (count == expected && allCompleted)
                ? CandleStatus.Completed
                : CandleStatus.Provisional;
            result.Add(new Candle(symbol, targetTimeFrame, bucketStart.Value, open, high, low, close, volume, status));
        }

        foreach (var c in ordered)
        {
            var start = TimeFrameBoundary.BucketStart(c.OpenTimeUtc, targetTimeFrame);
            if (bucketStart is null || start != bucketStart.Value)
            {
                Flush();
                bucketStart = start;
                open = c.Open; high = c.High; low = c.Low; close = c.Close; volume = c.Volume;
                count = 1;
                allCompleted = c.Status == CandleStatus.Completed;
            }
            else
            {
                if (c.High > high) high = c.High;
                if (c.Low < low) low = c.Low;
                close = c.Close;
                volume += c.Volume;
                count++;
                if (c.Status != CandleStatus.Completed) allCompleted = false;
            }
        }
        Flush();
        return result;
    }
}
