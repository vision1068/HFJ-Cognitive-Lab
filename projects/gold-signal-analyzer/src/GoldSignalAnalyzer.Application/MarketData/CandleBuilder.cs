using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.MarketData;

/// <summary>
/// FR-11.3: builds OHLCV candles from raw ticks for a requested timeframe.
/// Price is the tick mid ((bid+ask)/2). Volume is the tick count in the bucket
/// (a genuine observed count — never a fabricated figure). The bucket that the
/// <c>asOf</c> instant falls inside is emitted as <see cref="CandleStatus.Provisional"/>
/// (still forming); every earlier bucket is <see cref="CandleStatus.Completed"/>.
/// Empty buckets are never fabricated — a gap simply produces no candle.
/// </summary>
public static class CandleBuilder
{
    public static IReadOnlyList<Candle> Build(
        NormalizedSymbol symbol,
        TimeFrame timeFrame,
        IEnumerable<MarketTick> ticks,
        DateTimeOffset asOfUtc)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        if (ticks is null) throw new ArgumentNullException(nameof(ticks));
        var asOf = asOfUtc.ToUniversalTime();

        var ordered = ticks
            .Where(t => t.Symbol.Value == symbol.Value)
            .OrderBy(t => t.TimestampUtc)
            .ToList();

        var result = new List<Candle>();
        if (ordered.Count == 0) return result;

        DateTimeOffset? bucketStart = null;
        decimal open = 0, high = 0, low = 0, close = 0;
        long count = 0;

        void Flush()
        {
            if (bucketStart is null) return;
            var end = TimeFrameBoundary.BucketEnd(bucketStart.Value, timeFrame);
            var status = end <= asOf ? CandleStatus.Completed : CandleStatus.Provisional;
            result.Add(new Candle(symbol, timeFrame, bucketStart.Value, open, high, low, close, count, status));
        }

        foreach (var tick in ordered)
        {
            var start = TimeFrameBoundary.BucketStart(tick.TimestampUtc, timeFrame);
            var price = tick.Mid;
            if (bucketStart is null || start != bucketStart.Value)
            {
                Flush();
                bucketStart = start;
                open = high = low = close = price;
                count = 1;
            }
            else
            {
                if (price > high) high = price;
                if (price < low) low = price;
                close = price;
                count++;
            }
        }
        Flush();
        return result;
    }
}
