using GoldSignalAnalyzer.Desktop.Presentation.Screens;

namespace GoldSignalAnalyzer.Desktop.Presentation;

/// <summary>
/// The ordered presentation-layer registry of the 17 §30 placeholder screen view-model TYPES
/// (arch §7). This is the single source that drives DI registration, the shell nav-item list and
/// the navigation/DataTemplate coverage tests. The i-th type here corresponds to the i-th title in
/// <see cref="Section30Screens.Titles"/>; the R2 test locks that correspondence (identity + order).
/// </summary>
public static class ScreenCatalog
{
    /// <summary>The 17 screen view-model types, in authoritative §30 order.</summary>
    public static IReadOnlyList<Type> ViewModelTypes { get; } = new[]
    {
        typeof(DashboardHomeViewModel),
        typeof(ConnectionWizardViewModel),
        typeof(SymbolSelectionMappingViewModel),
        typeof(AdvancedChartViewModel),
        typeof(IndicatorPanelViewModel),
        typeof(SignalDetailViewModel),
        typeof(MarketRegimeViewModel),
        typeof(RiskPlanViewModel),
        typeof(NewsCalendarFilterViewModel),
        typeof(NotificationsCenterViewModel),
        typeof(BacktestingSetupViewModel),
        typeof(BacktestResultsViewModel),
        typeof(PaperTradingViewModel),
        typeof(TradeJournalViewModel),
        typeof(SettingsViewModel),
        typeof(DiagnosticsLogsViewModel),
        typeof(DisclaimerAboutViewModel),
    };
}
