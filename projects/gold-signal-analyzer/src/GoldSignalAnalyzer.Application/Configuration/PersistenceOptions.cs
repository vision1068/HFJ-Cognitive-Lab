namespace GoldSignalAnalyzer.Application.Configuration;

/// <summary>
/// Persistence options (gate B1). DbPath is bound from config with a per-user AppData default;
/// tests supply a scratch temp path so no test ever touches the real user DB.
/// </summary>
public sealed class PersistenceOptions
{
    public string DbPath { get; set; } = string.Empty;
}
