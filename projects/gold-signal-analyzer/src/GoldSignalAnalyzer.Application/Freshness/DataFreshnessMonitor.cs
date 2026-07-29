using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Freshness;

/// <summary>Assessment produced by <see cref="DataFreshnessMonitor"/>.</summary>
public sealed record FreshnessAssessment(FreshnessStatus Status, TimeSpan Age, DateTimeOffset? LastTickUtc);

/// <summary>
/// FR-12: tracks how old the last observed market data is and classifies it
/// Fresh / Stale / Unknown. The default before any tick is <c>Unknown</c>,
/// which the signal gate treats as NOT fresh — safety defaults to paused.
/// A tick timestamped in the future (beyond a small skew tolerance) is treated
/// as invalid → Stale, never Fresh (NFR-5: never trust a bad clock as live).
/// </summary>
public sealed class DataFreshnessMonitor
{
    private readonly IClock _clock;
    private readonly TimeSpan _staleAfter;
    private readonly TimeSpan _futureSkewTolerance;
    private DateTimeOffset? _lastTickUtc;

    public DataFreshnessMonitor(IClock clock, TimeSpan staleAfter, TimeSpan? futureSkewTolerance = null)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        if (staleAfter <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(staleAfter), "Stale threshold must be positive.");
        _staleAfter = staleAfter;
        _futureSkewTolerance = futureSkewTolerance ?? TimeSpan.FromSeconds(2);
    }

    /// <summary>Records an observed tick timestamp. Only advances forward so a
    /// late-arriving old tick cannot make data look staler than it is.</summary>
    public void RecordTick(DateTimeOffset tickUtc)
    {
        var utc = tickUtc.ToUniversalTime();
        if (_lastTickUtc is null || utc > _lastTickUtc) _lastTickUtc = utc;
    }

    /// <summary>Clears freshness state (e.g. on disconnect) → back to Unknown.</summary>
    public void Reset() => _lastTickUtc = null;

    public FreshnessAssessment Assess()
    {
        if (_lastTickUtc is null)
            return new FreshnessAssessment(FreshnessStatus.Unknown, TimeSpan.Zero, null);

        var age = _clock.UtcNow - _lastTickUtc.Value;

        // Future-dated beyond tolerance → invalid, not fresh.
        if (age < -_futureSkewTolerance)
            return new FreshnessAssessment(FreshnessStatus.Stale, age, _lastTickUtc);

        var status = age > _staleAfter ? FreshnessStatus.Stale : FreshnessStatus.Fresh;
        return new FreshnessAssessment(status, age, _lastTickUtc);
    }
}
