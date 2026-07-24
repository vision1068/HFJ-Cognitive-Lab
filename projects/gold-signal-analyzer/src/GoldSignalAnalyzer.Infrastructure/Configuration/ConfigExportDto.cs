namespace GoldSignalAnalyzer.Infrastructure.Configuration;

/// <summary>
/// Whitelist projection of <c>AppConfiguration</c> for export (arch §9, FR-6, NFR-1). Because this
/// DTO has NO secret property, export is incapable of emitting one. A reflection test (gate B3)
/// asserts no property name matches the secret denylist.
/// </summary>
public sealed class ConfigExportDto
{
    public string DisplayTimeZone { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string StartupScreen { get; set; } = string.Empty;
    public string Mt5TerminalPath { get; set; } = string.Empty;
    public string ConnectionMode { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string PreferredGoldSymbol { get; set; } = string.Empty;
    public List<string> EnabledTimeframes { get; set; } = new();
    public int BuyThreshold { get; set; }
    public int SellThreshold { get; set; }
    public int WinningMargin { get; set; }
    public bool ToastEnabled { get; set; }
    public bool SoundEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool TelegramEnabled { get; set; }
    public int SignalHistoryDays { get; set; }
    public int JournalRetentionDays { get; set; }
}
