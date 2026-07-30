using GoldSignalAnalyzer.Application.MarketData;
using GoldSignalAnalyzer.Domain;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// Phase 3 — Market Data Normalization (FR-11.3 / FR-11.4 / FR-11.5).
public class MarketDataNormalizationTests
{
    private static readonly NormalizedSymbol Gold = NormalizedSymbol.Gold;
    private static MarketTick Tick(DateTimeOffset ts, decimal price)
        => new(Gold, price, price, ts); // zero-spread → mid == price for exact assertions

    private static DateTimeOffset Utc(int h, int m, int s = 0)
        => new(2026, 7, 30, h, m, s, TimeSpan.Zero);

    // ---- FR-11.3/11.4: boundary alignment ----

    [Fact]
    public void D1_bucket_aligns_to_utc_midnight()
    {
        var b = TimeFrameBoundary.BucketStart(new DateTimeOffset(2026, 7, 30, 13, 47, 12, TimeSpan.Zero), TimeFrame.D1);
        Assert.Equal(new DateTimeOffset(2026, 7, 30, 0, 0, 0, TimeSpan.Zero), b);
    }

    [Theory]
    [InlineData(13, 47, 12, 12)]  // 13:47 → H4 bucket 12:00
    [InlineData(3, 59, 59, 0)]    // 03:59 → H4 bucket 00:00
    [InlineData(20, 0, 0, 20)]    // 20:00 → H4 bucket 20:00
    public void H4_bucket_aligns_to_4h_grid(int h, int m, int s, int expectedHour)
    {
        var b = TimeFrameBoundary.BucketStart(Utc(h, m, s), TimeFrame.H4);
        Assert.Equal(Utc(expectedHour, 0, 0), b);
    }

    [Fact]
    public void M5_bucket_floors_to_five_minute_grid()
    {
        var b = TimeFrameBoundary.BucketStart(Utc(12, 7, 33), TimeFrame.M5);
        Assert.Equal(Utc(12, 5, 0), b);
    }

    [Fact]
    public void CandlesPerBucket_computes_multiple_and_rejects_bad_pairs()
    {
        Assert.Equal(5, TimeFrameBoundary.CandlesPerBucket(TimeFrame.M1, TimeFrame.M5));
        Assert.Equal(4, TimeFrameBoundary.CandlesPerBucket(TimeFrame.M15, TimeFrame.H1));
        Assert.Equal(60, TimeFrameBoundary.CandlesPerBucket(TimeFrame.M1, TimeFrame.H1));
        Assert.Throws<ArgumentException>(() => TimeFrameBoundary.CandlesPerBucket(TimeFrame.H1, TimeFrame.M5));
        Assert.Throws<ArgumentException>(() => TimeFrameBoundary.CandlesPerBucket(TimeFrame.M5, TimeFrame.M5));
    }

    // ---- FR-11.3: build candles from ticks ----

    [Fact]
    public void Build_aggregates_ticks_into_ohlcv_with_provisional_tail()
    {
        var ticks = new[]
        {
            // bucket A: 12:00–12:05
            Tick(Utc(12, 0, 30), 2000m),
            Tick(Utc(12, 2, 0), 2010m),
            Tick(Utc(12, 4, 0), 1995m),
            Tick(Utc(12, 4, 59), 2005m),
            // bucket B: 12:05–12:10
            Tick(Utc(12, 6, 0), 2005m),
            Tick(Utc(12, 9, 0), 2020m),
        };
        // asOf inside bucket B → A completed, B provisional
        var candles = CandleBuilder.Build(Gold, TimeFrame.M5, ticks, Utc(12, 8, 0));

        Assert.Equal(2, candles.Count);

        var a = candles[0];
        Assert.Equal(Utc(12, 0, 0), a.OpenTimeUtc);
        Assert.Equal(2000m, a.Open);
        Assert.Equal(2010m, a.High);
        Assert.Equal(1995m, a.Low);
        Assert.Equal(2005m, a.Close);
        Assert.Equal(4m, a.Volume);
        Assert.Equal(CandleStatus.Completed, a.Status);

        var b = candles[1];
        Assert.Equal(Utc(12, 5, 0), b.OpenTimeUtc);
        Assert.Equal(2005m, b.Open);
        Assert.Equal(2020m, b.High);
        Assert.Equal(2005m, b.Low);
        Assert.Equal(2020m, b.Close);
        Assert.Equal(2m, b.Volume);
        Assert.Equal(CandleStatus.Provisional, b.Status);
    }

    [Fact]
    public void Build_never_fabricates_empty_buckets()
    {
        // Two ticks 30 minutes apart on M5 → only the two occupied buckets exist, no gap-fill.
        var ticks = new[] { Tick(Utc(12, 0, 0), 2000m), Tick(Utc(12, 30, 0), 2001m) };
        var candles = CandleBuilder.Build(Gold, TimeFrame.M5, ticks, Utc(13, 0, 0));
        Assert.Equal(2, candles.Count);
        Assert.All(candles, c => Assert.Equal(CandleStatus.Completed, c.Status));
    }

    // ---- FR-11.4/11.5: aggregate lower TF into higher TF ----

    private static Candle M1(int min, decimal o, decimal h, decimal l, decimal c, decimal v, CandleStatus st = CandleStatus.Completed)
        => new(Gold, TimeFrame.M1, Utc(12, min), o, h, l, c, v, st);

    [Fact]
    public void Aggregate_m1_into_m5_computes_ohlcv_and_completeness()
    {
        var m1 = new[]
        {
            M1(0, 2000, 2005, 1999, 2003, 10),
            M1(1, 2003, 2008, 2002, 2007, 12),
            M1(2, 2007, 2010, 2001, 2004, 8),
            M1(3, 2004, 2006, 2000, 2002, 9),
            M1(4, 2002, 2004, 1998, 2001, 11),
            // lone candle in next M5 bucket → provisional (only 1 of 5)
            M1(5, 2001, 2003, 2000, 2002, 7),
        };
        var m5 = CandleAggregator.Aggregate(Gold, TimeFrame.M1, TimeFrame.M5, m1);

        Assert.Equal(2, m5.Count);

        var full = m5[0];
        Assert.Equal(Utc(12, 0), full.OpenTimeUtc);
        Assert.Equal(2000m, full.Open);
        Assert.Equal(2010m, full.High);
        Assert.Equal(1998m, full.Low);
        Assert.Equal(2001m, full.Close);
        Assert.Equal(50m, full.Volume);
        Assert.Equal(CandleStatus.Completed, full.Status);

        var partial = m5[1];
        Assert.Equal(Utc(12, 5), partial.OpenTimeUtc);
        Assert.Equal(CandleStatus.Provisional, partial.Status);
        Assert.Equal(7m, partial.Volume);
    }

    [Fact]
    public void Aggregate_marks_provisional_if_any_source_candle_provisional()
    {
        var m1 = new[]
        {
            M1(0, 2000, 2005, 1999, 2003, 10),
            M1(1, 2003, 2008, 2002, 2007, 12),
            M1(2, 2007, 2010, 2001, 2004, 8),
            M1(3, 2004, 2006, 2000, 2002, 9),
            M1(4, 2002, 2004, 1998, 2001, 11, CandleStatus.Provisional), // last still forming
        };
        var m5 = CandleAggregator.Aggregate(Gold, TimeFrame.M1, TimeFrame.M5, m1);
        Assert.Single(m5);
        Assert.Equal(CandleStatus.Provisional, m5[0].Status);
    }

    [Fact]
    public void MultiTimeFrameSet_completed_view_excludes_provisional()
    {
        var candles = new List<Candle>
        {
            M1(0, 2000, 2005, 1999, 2003, 10),
            M1(1, 2003, 2008, 2002, 2007, 12, CandleStatus.Provisional),
        };
        var set = new MultiTimeFrameSet(new Dictionary<TimeFrame, IReadOnlyList<Candle>>
        {
            [TimeFrame.M1] = candles
        });
        Assert.Equal(2, set.All(TimeFrame.M1).Count);
        Assert.Single(set.Completed(TimeFrame.M1));
        Assert.NotNull(set.Provisional(TimeFrame.M1));
    }
}
