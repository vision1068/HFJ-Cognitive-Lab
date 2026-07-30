using GoldSignalAnalyzer.Presentation.Mvvm;

namespace GoldSignalAnalyzer.Presentation;

/// <summary>
/// Root view-model for the dashboard shell. Exposes the persistent, always-visible
/// "not investment advice" banner (FR-35.2), the current signal, and the read-only
/// paper journal. It composes child view-models only — no trading/execution surface.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    public MainViewModel(SignalViewModel signal, JournalViewModel journal)
    {
        Signal = signal ?? throw new ArgumentNullException(nameof(signal));
        Journal = journal ?? throw new ArgumentNullException(nameof(journal));
    }

    /// <summary>FR-35.2: structurally always present in the window chrome.</summary>
    public string DisclaimerBanner => DisclaimerText.Banner;

    public string Title => "Gold Signal Analyzer — Paper / Educational";

    public SignalViewModel Signal { get; }
    public JournalViewModel Journal { get; }
}
