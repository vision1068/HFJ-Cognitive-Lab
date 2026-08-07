namespace GoldSignalAnalyzer.Application.Abstractions;

/// <summary>
/// Time source. Injected everywhere freshness/backoff math needs "now" so
/// those calculations are unit-testable without a live terminal (NFR-10).
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
