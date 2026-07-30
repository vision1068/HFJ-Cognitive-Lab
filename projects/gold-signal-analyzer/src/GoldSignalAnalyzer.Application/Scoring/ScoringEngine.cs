using GoldSignalAnalyzer.Application.Indicators;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Scoring;

/// <summary>
/// FR-16/FR-18: turns an indicator snapshot into an independent BuyScore and
/// SellScore (each 0–100). Bull evidence feeds ONLY the buy pool and bear
/// evidence ONLY the sell pool, so the two scores are genuinely independent —
/// they are never forced to sum to 100 (proved by test). Each category's total
/// contribution is capped (FR-15) before the regime weight (FR-17) is applied,
/// and every step is recorded on the <see cref="ScoreContribution"/> for
/// traceability (INV-5). Pure function of its inputs (FR-24 determinism).
/// </summary>
public sealed class ScoringEngine
{
    private readonly ScoringConfig _cfg;
    public ScoringEngine(ScoringConfig? cfg = null) => _cfg = cfg ?? new ScoringConfig();

    public ScoreResult Score(
        IndicatorSnapshot snap,
        decimal lastClose,
        MarketRegime regime,
        IReadOnlyList<Penalty>? penalties = null,
        IReadOnlyList<Veto>? vetoes = null)
    {
        if (snap is null) throw new ArgumentNullException(nameof(snap));
        var raw = new List<(string ind, IndicatorCategory cat, SignalDirection dir, decimal pts, string note)>();

        void Rule(string ind, IndicatorCategory cat, SignalDirection dir, decimal pts, string note)
        {
            if (dir != SignalDirection.Neutral && pts > 0) raw.Add((ind, cat, dir, pts, note));
        }

        // R1 — EMA fast/slow cross (trend)
        if (snap.Value("EMA_FAST") is decimal fast && snap.Value("EMA_SLOW") is decimal slow)
        {
            if (fast > slow) Rule("EMA_CROSS", IndicatorCategory.Trend, SignalDirection.Buy, _cfg.EmaCrossPoints, $"EMA fast {fast:0.##} > slow {slow:0.##}");
            else if (fast < slow) Rule("EMA_CROSS", IndicatorCategory.Trend, SignalDirection.Sell, _cfg.EmaCrossPoints, $"EMA fast {fast:0.##} < slow {slow:0.##}");
        }

        // R2 — MACD histogram (trend)
        if (snap.Value("MACD_HIST") is decimal hist)
        {
            if (hist > 0) Rule("MACD_HIST", IndicatorCategory.Trend, SignalDirection.Buy, _cfg.MacdPoints, $"MACD histogram {hist:0.####} > 0");
            else if (hist < 0) Rule("MACD_HIST", IndicatorCategory.Trend, SignalDirection.Sell, _cfg.MacdPoints, $"MACD histogram {hist:0.####} < 0");
        }

        // R3 — ADX directional trend (trend), only counted when a trend actually exists
        if (snap.Value("ADX") is decimal adx && adx >= _cfg.AdxTrendThreshold
            && snap.Value("PLUS_DI") is decimal pdi && snap.Value("MINUS_DI") is decimal mdi)
        {
            if (pdi > mdi) Rule("ADX_DI", IndicatorCategory.Trend, SignalDirection.Buy, _cfg.AdxTrendPoints, $"ADX {adx:0.#} trend, +DI>{mdi:0.#}");
            else if (mdi > pdi) Rule("ADX_DI", IndicatorCategory.Trend, SignalDirection.Sell, _cfg.AdxTrendPoints, $"ADX {adx:0.#} trend, -DI>{pdi:0.#}");
        }

        // R4 — RSI momentum
        if (snap.Value("RSI") is decimal rsi)
        {
            if (rsi >= 70m) Rule("RSI", IndicatorCategory.Momentum, SignalDirection.Buy, _cfg.RsiStrongPoints, $"RSI {rsi:0.#} strong bull");
            else if (rsi >= 55m) Rule("RSI", IndicatorCategory.Momentum, SignalDirection.Buy, _cfg.RsiPoints, $"RSI {rsi:0.#} bull");
            else if (rsi <= 30m) Rule("RSI", IndicatorCategory.Momentum, SignalDirection.Sell, _cfg.RsiStrongPoints, $"RSI {rsi:0.#} strong bear");
            else if (rsi <= 45m) Rule("RSI", IndicatorCategory.Momentum, SignalDirection.Sell, _cfg.RsiPoints, $"RSI {rsi:0.#} bear");
        }

        // R5 — Stochastic momentum
        if (snap.Value("STOCH_K") is decimal k)
        {
            if (k >= 55m) Rule("STOCH", IndicatorCategory.Momentum, SignalDirection.Buy, _cfg.StochPoints, $"Stoch %K {k:0.#} bull");
            else if (k <= 45m) Rule("STOCH", IndicatorCategory.Momentum, SignalDirection.Sell, _cfg.StochPoints, $"Stoch %K {k:0.#} bear");
        }

        // R6 — price vs Bollinger middle (volatility bias)
        if (snap.Value("BB_MIDDLE") is decimal mid)
        {
            if (lastClose > mid) Rule("BB_POS", IndicatorCategory.Volatility, SignalDirection.Buy, _cfg.BollingerPoints, $"price {lastClose:0.##} above BB mid {mid:0.##}");
            else if (lastClose < mid) Rule("BB_POS", IndicatorCategory.Volatility, SignalDirection.Sell, _cfg.BollingerPoints, $"price {lastClose:0.##} below BB mid {mid:0.##}");
        }

        // Apply category caps (FR-15) in order, then regime weights (FR-17).
        var weights = _cfg.WeightsFor(regime);
        var used = new Dictionary<(IndicatorCategory, SignalDirection), decimal>();
        var contributions = new List<ScoreContribution>();
        foreach (var r in raw)
        {
            decimal cap = _cfg.CapFor(r.cat);
            var key = (r.cat, r.dir);
            decimal already = used.GetValueOrDefault(key);
            decimal capped = Math.Max(0m, Math.Min(r.pts, cap - already));
            used[key] = already + capped;
            decimal weight = weights.For(r.cat);
            contributions.Add(new ScoreContribution(
                r.ind, r.cat, r.dir, r.pts, cap, capped, weight, capped * weight, r.note));
        }

        decimal maxWeighted =
            _cfg.TrendCap * weights.Trend + _cfg.MomentumCap * weights.Momentum +
            _cfg.VolatilityCap * weights.Volatility + _cfg.VolumeCap * weights.Volume;

        decimal Normalize(SignalDirection d)
        {
            if (maxWeighted <= 0) return 0m;
            decimal sum = contributions.Where(c => c.Direction == d).Sum(c => c.WeightedPoints);
            return Math.Clamp(Math.Round(sum / maxWeighted * 100m, 2), 0m, 100m);
        }

        return new ScoreResult(
            Normalize(SignalDirection.Buy),
            Normalize(SignalDirection.Sell),
            regime,
            weights,
            contributions,
            penalties ?? Array.Empty<Penalty>(),
            vetoes ?? Array.Empty<Veto>());
    }
}
