namespace GoldSignalAnalyzer.Desktop.Presentation;

/// <summary>A single entry in the shell navigation rail: a display title bound to a screen VM type.</summary>
public sealed record NavItem(string Title, Type ViewModelType);
