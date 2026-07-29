using GoldSignalAnalyzer.Application.Abstractions;

namespace GoldSignalAnalyzer.Infrastructure.Time;

/// <summary>Production clock — wall-clock UTC.</summary>
public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
