using System.Text.Json;
using GoldSignalAnalyzer.Application.Configuration;

namespace GoldSignalAnalyzer.Infrastructure.Configuration;

/// <summary>
/// Exports non-sensitive configuration (FR-6, NFR-1). Serializes the whitelist
/// <see cref="ConfigExportDto"/> only — there is no secret property to leak.
/// </summary>
public sealed class ConfigExportService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public ConfigExportDto ToDto(AppConfiguration c) => new()
    {
        DisplayTimeZone = c.General.DisplayTimeZone,
        Theme = c.General.Theme,
        StartupScreen = c.General.StartupScreen,
        Mt5TerminalPath = c.Connection.Mt5TerminalPath,
        ConnectionMode = c.Connection.ConnectionMode.ToString(),
        ServerName = c.Connection.ServerName,
        PreferredGoldSymbol = c.Symbols.PreferredGoldSymbol,
        EnabledTimeframes = c.Timeframes.EnabledTimeframes.Select(t => t.ToString()).ToList(),
        BuyThreshold = c.Scoring.BuyThreshold,
        SellThreshold = c.Scoring.SellThreshold,
        WinningMargin = c.Scoring.WinningMargin,
        ToastEnabled = c.Notifications.ToastEnabled,
        SoundEnabled = c.Notifications.SoundEnabled,
        EmailEnabled = c.Notifications.EmailEnabled,
        TelegramEnabled = c.Notifications.TelegramEnabled,
        SignalHistoryDays = c.DataRetention.SignalHistoryDays,
        JournalRetentionDays = c.DataRetention.JournalRetentionDays
    };

    public string ExportJson(AppConfiguration c) => JsonSerializer.Serialize(ToDto(c), Options);
}
