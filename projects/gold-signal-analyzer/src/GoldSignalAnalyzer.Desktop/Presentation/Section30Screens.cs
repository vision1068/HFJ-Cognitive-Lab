namespace GoldSignalAnalyzer.Desktop.Presentation;

/// <summary>
/// The literal, authoritative source-spec §30 screen list, checked into the repo (gate R2, arch §7).
/// This is the single reference the R2 mapping test reconciles the 17 registered screen view-models
/// against — no missing, no extra. Order and titles are byte-authoritative for Cycle 1.
/// </summary>
public static class Section30Screens
{
    /// <summary>The 17 §30 screen titles in authoritative order.</summary>
    public static IReadOnlyList<string> Titles { get; } = new[]
    {
        "Dashboard / Home",
        "Connection Wizard (MT5 setup)",
        "Symbol Selection & Mapping",
        "Advanced Chart",
        "Indicator Panel",
        "Signal Detail / Explanation",
        "Market Regime",
        "Risk Plan",
        "News / Economic Calendar Filter",
        "Notifications Center",
        "Backtesting Setup",
        "Backtest Results / Report",
        "Paper Trading",
        "Trade Journal",
        "Settings",
        "Diagnostics / Logs export",
        "Disclaimer / About",
    };
}
