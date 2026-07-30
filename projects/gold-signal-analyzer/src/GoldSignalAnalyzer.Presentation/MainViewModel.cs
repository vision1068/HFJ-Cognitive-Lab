using GoldSignalAnalyzer.Presentation.Mvvm;

namespace GoldSignalAnalyzer.Presentation;

/// <summary>
/// Root view-model for the dashboard shell. Exposes the persistent, always-visible
/// "not investment advice" banner (FR-35.2), the current signal, and the read-only
/// paper journal. It composes child view-models only — no trading/execution surface.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    public MainViewModel(
        SignalViewModel signal,
        JournalViewModel journal,
        ChartViewModel chart,
        SignalNotifier notifier)
    {
        Signal = signal ?? throw new ArgumentNullException(nameof(signal));
        Journal = journal ?? throw new ArgumentNullException(nameof(journal));
        Chart = chart ?? throw new ArgumentNullException(nameof(chart));
        Notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
    }

    /// <summary>FR-35.2: structurally always present in the window chrome.</summary>
    public string DisclaimerBanner => DisclaimerText.Banner;

    public string Title => "Gold Signal Analyzer — Paper / Educational";

    public SignalViewModel Signal { get; }
    public JournalViewModel Journal { get; }

    /// <summary>FR-27: candle chart + indicator overlays (read-only drawing).</summary>
    public ChartViewModel Chart { get; }

    /// <summary>FR-30: in-app signal notifications (passive alerts, no action).</summary>
    public SignalNotifier Notifier { get; }
}
