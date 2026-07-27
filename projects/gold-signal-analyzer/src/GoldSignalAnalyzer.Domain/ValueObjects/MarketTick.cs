namespace GoldSignalAnalyzer.Domain.ValueObjects;

/// <summary>
/// A single bid/ask quote (spec §9 <c>MarketTick</c>; renamed from the Cycle-1 <c>Tick</c>).
/// Time is UTC (NFR-9). Immutable value object streamed by <c>IMarketDataProvider.StreamTicksAsync</c>.
/// </summary>
public sealed record MarketTick(
    string Symbol,
    decimal Bid,
    decimal Ask,
    DateTime TimeUtc);
