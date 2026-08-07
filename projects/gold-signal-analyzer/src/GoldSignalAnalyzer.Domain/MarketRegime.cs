namespace GoldSignalAnalyzer.Domain;

/// <summary>
/// FR-17: coarse market regime used to adjust category weights. A trend-follow
/// signal is weighted up in a trending regime and down in a ranging one.
/// </summary>
public enum MarketRegime
{
    Ranging = 0,
    TrendingUp = 1,
    TrendingDown = 2,
    Volatile = 3
}
