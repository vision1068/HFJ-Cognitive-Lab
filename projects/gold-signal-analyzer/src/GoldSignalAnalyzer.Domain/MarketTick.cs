namespace GoldSignalAnalyzer.Domain;

/// <summary>
/// A single bid/ask observation for a symbol at a point in time. Timestamps
/// are always UTC (freshness math must not depend on a machine's local zone).
/// Prices are decimal — never float/double — because these are money.
/// </summary>
public sealed record MarketTick
{
    public NormalizedSymbol Symbol { get; }
    public decimal Bid { get; }
    public decimal Ask { get; }

    /// <summary>UTC instant the tick was produced by the source.</summary>
    public DateTimeOffset TimestampUtc { get; }

    public decimal Mid => (Bid + Ask) / 2m;
    public decimal Spread => Ask - Bid;

    public MarketTick(NormalizedSymbol symbol, decimal bid, decimal ask, DateTimeOffset timestampUtc)
    {
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        if (bid <= 0m) throw new ArgumentOutOfRangeException(nameof(bid), "Bid must be positive.");
        if (ask <= 0m) throw new ArgumentOutOfRangeException(nameof(ask), "Ask must be positive.");
        if (ask < bid) throw new ArgumentException("Ask cannot be below bid.", nameof(ask));
        Bid = bid;
        Ask = ask;
        TimestampUtc = timestampUtc.ToUniversalTime();
    }
}
