namespace GoldSignalAnalyzer.Domain;

/// <summary>
/// FR-15: the family an indicator belongs to. Scoring caps total contribution
/// per category so several correlated indicators (e.g. three trend measures)
/// cannot be double-counted into an inflated score.
/// </summary>
public enum IndicatorCategory
{
    Trend = 0,
    Momentum = 1,
    Volatility = 2,
    Volume = 3
}
