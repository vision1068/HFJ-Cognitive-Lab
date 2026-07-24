namespace GoldSignalAnalyzer.Desktop.Presentation.Screens;

/// <summary>
/// Base for the 17 §30 Cycle-1 placeholder screens (arch §7). Each concrete screen is a navigable
/// VM+View pair registered in DI. Every screen carries an honest <see cref="RoadmapBanner"/> so the
/// shell is demoable without pretending capability that does not yet exist (arch §7, RM4/RM5 spirit).
/// </summary>
public abstract class PlaceholderScreenViewModel : ViewModelBase
{
    /// <summary>Honesty banner shown on the screen — states the feature is a roadmap placeholder.</summary>
    public string RoadmapBanner { get; }

    /// <summary>Requirement/section this placeholder traces to in the source spec (arch §7 table).</summary>
    public string Traceability { get; }

    protected PlaceholderScreenViewModel(string title, string traceability, string? roadmapBanner = null)
    {
        Title = title;
        Traceability = traceability;
        RoadmapBanner = roadmapBanner
            ?? "Roadmap placeholder — not yet implemented (Cycle 2+). This screen is navigable now; its capability lands in a later cycle.";
    }
}
