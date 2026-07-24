using GoldSignalAnalyzer.Domain.Enums;

namespace GoldSignalAnalyzer.Domain.ValueObjects;

/// <summary>An OHLCV candle. All times are UTC (NFR-9). Immutable value object.</summary>
public sealed record Candle(
    DateTime OpenTimeUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    double Volume,
    Timeframe Timeframe,
    bool IsClosed);
