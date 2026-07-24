namespace GoldSignalAnalyzer.Application.Ports;

/// <summary>Lightweight symbol descriptor for broker symbol detection (spec §7, FR-9).</summary>
public sealed record SymbolInfo(string Name, string Description);
