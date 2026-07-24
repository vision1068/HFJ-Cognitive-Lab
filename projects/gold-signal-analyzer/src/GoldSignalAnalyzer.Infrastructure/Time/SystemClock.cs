using GoldSignalAnalyzer.Application.Abstractions;

namespace GoldSignalAnalyzer.Infrastructure.Time;

/// <summary>UTC system clock (NFR-9).</summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
