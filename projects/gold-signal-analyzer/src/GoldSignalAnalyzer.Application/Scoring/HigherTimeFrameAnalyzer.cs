using GoldSignalAnalyzer.Application.Indicators;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Scoring;

/// <summary>
/// Cycle 9 (FR-41/FR-42): derives a genuine higher-timeframe (HTF) confirmation
/// direction from an HTF candle series, so the FR-21 HTF guard performs a REAL check.
///
/// It reuses the existing, individually-tested analytical stages — IndicatorEngine →
/// RegimeClassifier → ScoringEngine — and takes the higher timeframe's INDEPENDENT
/// directional lean, exactly the basis <see cref="SignalClassifier"/> uses to compute
/// the lower-timeframe <c>proposed</c> direction (BuyScore vs SellScore). "Confirmation"
/// therefore means: does the higher timeframe lean the same way?
///
/// It deliberately does NOT run <see cref="SignalClassifier"/> on the HTF series (that
/// would re-invoke the HTF guard → infinite regress); it is a pure trend/lean read.
///
/// Fail-safe (INV-4, D9-4): a null/empty series, or a genuine tie (no directional edge),
/// returns <c>null</c> = "no confirmation" — never a fabricated direction. Pure function
/// of its inputs (FR-24 determinism): a fresh engine is built each call, no shared state.
///
/// NOTE: this analyzer applies NO recency/staleness bound — it is candle-only. The
/// live-only "frozen/ancient HTF feed" bound (D9-11) lives in <c>LiveSignalCoordinator</c>,
/// which has a clock; the sample path uses deterministic historical demo data and must not
/// be subjected to a wall-clock recency check.
/// </summary>
public sealed class HigherTimeFrameAnalyzer
{
    private readonly IndicatorConfig _indicator;
    private readonly ScoringConfig _scoring;

    public HigherTimeFrameAnalyzer(IndicatorConfig? indicator = null, ScoringConfig? scoring = null)
    {
        _indicator = indicator ?? new IndicatorConfig();
        _scoring = scoring ?? new ScoringConfig();
    }

    /// <summary>
    /// The higher timeframe's directional lean, or <c>null</c> when there is no clear
    /// lean (tie) or no usable series (fail-safe → the guard will block).
    /// </summary>
    public SignalDirection? Confirm(
        NormalizedSymbol symbol, TimeFrame htfTimeFrame, IReadOnlyList<Candle>? htfCandles)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        if (htfCandles is null || htfCandles.Count == 0) return null; // fail-safe: no HTF read

        var snapshot = new IndicatorEngine(_indicator).Compute(symbol, htfTimeFrame, htfCandles);
        var regime = new RegimeClassifier(_scoring).Classify(snapshot);
        decimal lastClose = htfCandles[^1].Close;
        var score = new ScoringEngine(_scoring).Score(snapshot, lastClose, regime);

        // Same basis as SignalClassifier's `proposed` (SignalClassifier.cs:73):
        // the sign of the independent Buy/Sell score difference; a tie → null.
        if (score.BuyScore > score.SellScore) return SignalDirection.Buy;
        if (score.SellScore > score.BuyScore) return SignalDirection.Sell;
        return null;
    }
}
