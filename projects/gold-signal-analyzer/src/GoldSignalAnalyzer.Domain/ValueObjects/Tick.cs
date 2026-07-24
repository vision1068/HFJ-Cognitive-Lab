namespace GoldSignalAnalyzer.Domain.ValueObjects;

/// <summary>A single bid/ask quote. Time is UTC (NFR-9). Immutable value object.</summary>
public sealed record Tick(
    string Symbol,
    decimal Bid,
    decimal Ask,
    DateTime TimeUtc);
