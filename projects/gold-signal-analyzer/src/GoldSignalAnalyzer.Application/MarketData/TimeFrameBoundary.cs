using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.MarketData;

/// <summary>
/// FR-11.3 / FR-11.4: aligns timestamps to timeframe bucket boundaries.
/// Alignment is anchored on the Unix epoch (1970-01-01T00:00:00Z), which is a
/// whole multiple of every supported timeframe, so:
///   - intraday buckets fall on natural minute boundaries (M5 → :00,:05,…),
///   - H4 buckets fall on 00/04/08/12/16/20 UTC,
///   - D1 buckets fall on UTC midnight.
/// Uses integer tick math (no floating point) so boundaries are exact.
/// </summary>
public static class TimeFrameBoundary
{
    /// <summary>Start (inclusive) of the bucket that <paramref name="tsUtc"/> falls in.</summary>
    public static DateTimeOffset BucketStart(DateTimeOffset tsUtc, TimeFrame tf)
    {
        long tfTicks = (long)tf * TimeSpan.TicksPerMinute;
        long sinceEpoch = tsUtc.ToUniversalTime().UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks;
        long remainder = ((sinceEpoch % tfTicks) + tfTicks) % tfTicks; // floor even if negative
        long bucketTicks = sinceEpoch - remainder;
        return new DateTimeOffset(DateTimeOffset.UnixEpoch.UtcTicks + bucketTicks, TimeSpan.Zero);
    }

    /// <summary>End (exclusive) of the bucket that starts at <paramref name="bucketStartUtc"/>.</summary>
    public static DateTimeOffset BucketEnd(DateTimeOffset bucketStartUtc, TimeFrame tf)
        => bucketStartUtc.AddMinutes((int)tf);

    /// <summary>How many source-TF candles make up one target-TF candle.
    /// Throws if the target is not a whole multiple of the source.</summary>
    public static int CandlesPerBucket(TimeFrame source, TimeFrame target)
    {
        int s = (int)source, t = (int)target;
        if (t <= s) throw new ArgumentException($"Target timeframe {target} must be larger than source {source}.");
        if (t % s != 0) throw new ArgumentException($"Target timeframe {target} is not a whole multiple of source {source}.");
        return t / s;
    }
}
