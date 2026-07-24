namespace GoldSignalAnalyzer.Desktop.Presentation.Screens;

/// <summary>§30 screen "Disclaimer / About" (traces to FR-35, §40). Cycle-1 navigable placeholder (arch §7).</summary>
public sealed class DisclaimerAboutViewModel : PlaceholderScreenViewModel
{
    public DisclaimerAboutViewModel()
        : base("Disclaimer / About", "FR-35, §40", "Roadmap placeholder — not yet implemented (Cycle 2+). This is a navigation placeholder only; the blocking, versioned, audited disclaimer-acknowledgement gate (RM4) is a later cycle and is NOT wired here.")
    {
    }
}
