namespace GoldSignalAnalyzer.Domain;

/// <summary>
/// Data-freshness classification (FR-12). <see cref="Unknown"/> is the
/// safe default before any tick has been observed — treated as NOT fresh.
/// </summary>
public enum FreshnessStatus
{
    Unknown = 0,
    Fresh = 1,
    Stale = 2
}
