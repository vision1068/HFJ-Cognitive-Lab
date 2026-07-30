using GoldSignalAnalyzer.Application.Indicators;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Scoring;

/// <summary>
/// FR-17: classifies the market regime from indicator readings.
///   - ADX ≥ threshold ⇒ trending; +DI vs −DI picks up/down.
///   - otherwise, high Bollinger bandwidth (width/middle) ⇒ Volatile, else Ranging.
/// Deterministic and driven purely by real computed values.
/// </summary>
public sealed class RegimeClassifier
{
    private readonly ScoringConfig _cfg;
    public RegimeClassifier(ScoringConfig? cfg = null) => _cfg = cfg ?? new ScoringConfig();

    public MarketRegime Classify(IndicatorSnapshot snap)
    {
        decimal? adx = snap.Value("ADX");
        decimal? plusDi = snap.Value("PLUS_DI");
        decimal? minusDi = snap.Value("MINUS_DI");

        if (adx is decimal a && a >= _cfg.RegimeAdxTrendThreshold && plusDi is decimal p && minusDi is decimal m)
            return p >= m ? MarketRegime.TrendingUp : MarketRegime.TrendingDown;

        decimal? width = snap.Value("BB_WIDTH");
        decimal? mid = snap.Value("BB_MIDDLE");
        if (width is decimal w && mid is decimal mv && mv > 0 && (w / mv) > _cfg.VolatileBandwidthRatio)
            return MarketRegime.Volatile;

        return MarketRegime.Ranging;
    }
}
