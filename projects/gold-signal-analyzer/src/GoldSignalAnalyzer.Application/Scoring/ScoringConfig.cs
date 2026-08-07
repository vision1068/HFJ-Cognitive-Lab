using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Scoring;

/// <summary>
/// FR-18: all scoring knobs in one configurable place. Category caps prevent
/// several correlated indicators (FR-15) from double-counting; regime weight-sets
/// re-weight categories by regime (FR-17). Defaults are documented and modest.
/// </summary>
public sealed record ScoringConfig
{
    // Per-indicator raw point rules (FR-13 interpretation → directional points).
    public decimal EmaCrossPoints { get; init; } = 10m;
    public decimal MacdPoints { get; init; } = 10m;
    public decimal AdxTrendPoints { get; init; } = 15m;
    public decimal AdxTrendThreshold { get; init; } = 25m;
    public decimal RsiStrongPoints { get; init; } = 15m;
    public decimal RsiPoints { get; init; } = 8m;
    public decimal StochPoints { get; init; } = 8m;
    public decimal BollingerPoints { get; init; } = 6m;

    // Cycle 10 (FR-45): Fibonacci golden-pocket retracement confirmation.
    public decimal FibonacciPoints { get; init; } = 8m;
    public decimal FibonacciPocketLow { get; init; } = 0.382m;
    public decimal FibonacciPocketHigh { get; init; } = 0.618m;

    // Cycle 10 (FR-46): liquidity sweep (stop-hunt + rejection).
    public decimal LiquiditySweepPoints { get; init; } = 10m;

    // FR-15 category caps (max points contributed per category, per direction).
    public decimal TrendCap { get; init; } = 30m;
    public decimal MomentumCap { get; init; } = 20m;
    public decimal VolatilityCap { get; init; } = 10m;
    public decimal VolumeCap { get; init; } = 10m;

    public decimal CapFor(IndicatorCategory c) => c switch
    {
        IndicatorCategory.Trend => TrendCap,
        IndicatorCategory.Momentum => MomentumCap,
        IndicatorCategory.Volatility => VolatilityCap,
        IndicatorCategory.Volume => VolumeCap,
        _ => 0m
    };

    // FR-17 regime weight-sets.
    public CategoryWeights TrendingWeights { get; init; } = new(Trend: 1.5m, Momentum: 1.0m, Volatility: 0.5m, Volume: 1.0m);
    public CategoryWeights RangingWeights { get; init; } = new(Trend: 0.5m, Momentum: 1.5m, Volatility: 1.0m, Volume: 1.0m);
    public CategoryWeights VolatileWeights { get; init; } = new(Trend: 1.0m, Momentum: 1.0m, Volatility: 1.5m, Volume: 1.0m);

    public CategoryWeights WeightsFor(MarketRegime regime) => regime switch
    {
        MarketRegime.TrendingUp or MarketRegime.TrendingDown => TrendingWeights,
        MarketRegime.Ranging => RangingWeights,
        MarketRegime.Volatile => VolatileWeights,
        _ => RangingWeights
    };

    // Regime classification thresholds (FR-17).
    public decimal RegimeAdxTrendThreshold { get; init; } = 25m;
    /// <summary>Bollinger width / middle above this ⇒ Volatile when not trending.</summary>
    public decimal VolatileBandwidthRatio { get; init; } = 0.02m;
}
