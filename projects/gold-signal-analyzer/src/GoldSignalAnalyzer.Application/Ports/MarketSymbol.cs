namespace GoldSignalAnalyzer.Application.Ports;

/// <summary>
/// Lightweight symbol descriptor for broker symbol detection (spec §9 <c>MarketSymbol</c>; renamed
/// from the Cycle-1 <c>SymbolInfo</c>). See §7 / FR-9.
/// </summary>
public sealed record MarketSymbol(string Name, string Description);
