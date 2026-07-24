namespace GoldSignalAnalyzer.Persistence.Entities;

/// <summary>Non-secret key/value setting (arch §6). The repository rejects denylisted keys (gate B3).</summary>
public sealed class AppSetting : AuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
