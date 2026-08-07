namespace GoldSignalAnalyzer.Domain;

/// <summary>
/// Candle timeframes the bridge can request. Values are minutes-per-candle
/// so the bridge/provider layer can translate to a broker-native constant.
/// </summary>
public enum TimeFrame
{
    M1 = 1,
    M5 = 5,
    M15 = 15,
    M30 = 30,
    H1 = 60,
    H4 = 240,
    D1 = 1440
}
