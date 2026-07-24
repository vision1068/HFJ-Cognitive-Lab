namespace GoldSignalAnalyzer.Application.Abstractions;

/// <summary>UTC time source (NFR-9). All internal timestamps normalized to UTC.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
