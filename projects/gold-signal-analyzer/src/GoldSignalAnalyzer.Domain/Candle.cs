namespace GoldSignalAnalyzer.Domain;

/// <summary>
/// An OHLC candle. Enforces basic OHLC sanity at construction so a corrupt
/// bar can never enter the calculation layer (secure-by-construction).
/// </summary>
public sealed record Candle
{
    public NormalizedSymbol Symbol { get; }
    public TimeFrame TimeFrame { get; }
    public DateTimeOffset OpenTimeUtc { get; }
    public decimal Open { get; }
    public decimal High { get; }
    public decimal Low { get; }
    public decimal Close { get; }
    public decimal Volume { get; }

    public Candle(
        NormalizedSymbol symbol,
        TimeFrame timeFrame,
        DateTimeOffset openTimeUtc,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        decimal volume)
    {
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        if (open <= 0 || high <= 0 || low <= 0 || close <= 0)
            throw new ArgumentException("OHLC prices must all be positive.");
        if (high < low) throw new ArgumentException("High cannot be below low.");
        if (high < open || high < close) throw new ArgumentException("High must be the max of the bar.");
        if (low > open || low > close) throw new ArgumentException("Low must be the min of the bar.");
        if (volume < 0) throw new ArgumentOutOfRangeException(nameof(volume), "Volume cannot be negative.");
        TimeFrame = timeFrame;
        OpenTimeUtc = openTimeUtc.ToUniversalTime();
        Open = open; High = high; Low = low; Close = close; Volume = volume;
    }
}
