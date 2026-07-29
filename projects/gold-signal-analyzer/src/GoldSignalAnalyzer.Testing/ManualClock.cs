using GoldSignalAnalyzer.Application.Abstractions;

namespace GoldSignalAnalyzer.Testing;

/// <summary>Deterministic clock for tests. Advance time explicitly.</summary>
public sealed class ManualClock : IClock
{
    public ManualClock(DateTimeOffset start) => UtcNow = start.ToUniversalTime();
    public DateTimeOffset UtcNow { get; private set; }
    public void Advance(TimeSpan by) => UtcNow = UtcNow + by;
    public void Set(DateTimeOffset to) => UtcNow = to.ToUniversalTime();
}
