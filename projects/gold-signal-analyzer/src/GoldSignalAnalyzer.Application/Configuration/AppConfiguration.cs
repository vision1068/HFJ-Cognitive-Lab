using GoldSignalAnalyzer.Domain.Enums;

namespace GoldSignalAnalyzer.Application.Configuration;

/// <summary>
/// Bound application configuration (arch §9, spec §37). By DESIGN this model has NO credential
/// property anywhere — no Password/Token/ApiKey/InvestorPassword/TelegramBotToken/EmailPassword.
/// Channel settings are booleans only. Mt5TerminalPath and ServerName are non-secret operational
/// values (spec §44). A reflection test (gate B3) asserts no property name matches the denylist.
/// </summary>
public sealed class AppConfiguration
{
    public GeneralOptions General { get; set; } = new();
    public ConnectionOptions Connection { get; set; } = new();
    public SymbolsOptions Symbols { get; set; } = new();
    public TimeframesOptions Timeframes { get; set; } = new();
    public ScoringOptions Scoring { get; set; } = new();
    public NotificationsOptions Notifications { get; set; } = new();
    public DataRetentionOptions DataRetention { get; set; } = new();
}

public sealed class GeneralOptions
{
    public string DisplayTimeZone { get; set; } = "UTC";
    public string Theme { get; set; } = "Dark";
    public string StartupScreen { get; set; } = "Dashboard";
}

public sealed class ConnectionOptions
{
    /// <summary>Non-secret path to the local MT5 terminal (spec §44).</summary>
    public string Mt5TerminalPath { get; set; } = string.Empty;

    public ConnectionMode ConnectionMode { get; set; } = ConnectionMode.ModeA;

    /// <summary>Non-secret broker server name (spec §44).</summary>
    public string ServerName { get; set; } = string.Empty;
}

public sealed class SymbolsOptions
{
    public string PreferredGoldSymbol { get; set; } = "XAUUSD";
    public List<string> SymbolMappings { get; set; } = new();
}

public sealed class TimeframesOptions
{
    public List<Timeframe> EnabledTimeframes { get; set; } = new() { Timeframe.H1 };
}

public sealed class ScoringOptions
{
    public int BuyThreshold { get; set; } = 70;
    public int SellThreshold { get; set; } = 70;
    public int WinningMargin { get; set; } = 15;
}

public sealed class NotificationsOptions
{
    // Booleans ONLY. Channel secrets live in ICredentialStore (Cycle 2), never in config.
    public bool ToastEnabled { get; set; }
    public bool SoundEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool TelegramEnabled { get; set; }
}

public sealed class DataRetentionOptions
{
    public int SignalHistoryDays { get; set; } = 90;
    public int JournalRetentionDays { get; set; } = 365;
}
