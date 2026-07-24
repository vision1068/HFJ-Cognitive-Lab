namespace GoldSignalAnalyzer.Domain.Entities;

/// <summary>
/// Maps a broker-specific gold symbol variant (XAUUSD, GOLD, XAUUSDm, .a, .c, .pro, .raw ...)
/// to the normalized internal symbol (spec §7, FR-9). No hardcoded XAUUSD assumption.
/// </summary>
public sealed class SymbolMapping
{
    public string BrokerSymbol { get; init; } = string.Empty;
    public string NormalizedSymbol { get; init; } = "XAU/USD";
}
