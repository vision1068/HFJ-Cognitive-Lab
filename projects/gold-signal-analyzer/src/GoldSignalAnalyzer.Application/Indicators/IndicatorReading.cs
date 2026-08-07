using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Indicators;

/// <summary>
/// A single computed indicator fact. Carries its <see cref="Category"/> (FR-15
/// caps), the <see cref="TimeFrame"/> it was computed on, and whether it was
/// derived from a fully <see cref="CandleStatus.Completed"/> series or one that
/// still contains a forming (<see cref="CandleStatus.Provisional"/>) bar (FR-14).
/// This is a raw, uninterpreted value — direction/scoring is applied later so
/// no interpretation can leak fabricated facts into the reading itself.
/// </summary>
public sealed record IndicatorReading(
    string Name,
    IndicatorCategory Category,
    decimal Value,
    TimeFrame TimeFrame,
    CandleStatus Status);

/// <summary>The full indicator panel computed for one timeframe.</summary>
public sealed record IndicatorSnapshot(
    NormalizedSymbol Symbol,
    TimeFrame TimeFrame,
    CandleStatus Status,
    IReadOnlyList<IndicatorReading> Readings)
{
    public IndicatorReading? Get(string name) => Readings.FirstOrDefault(r => r.Name == name);
    public decimal? Value(string name) => Get(name)?.Value;
}
