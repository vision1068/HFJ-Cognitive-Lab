using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Indicators;
using GoldSignalAnalyzer.Application.Risk;
using GoldSignalAnalyzer.Application.Scoring;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Analysis;

/// <summary>
/// The result of analysing one candle series — everything the UI shell needs to
/// display, and nothing it must not. It is a read-only projection of values the
/// analytical core computed; it contains no live-order surface and no fabricated
/// field (INV-1/INV-4/INV-5).
/// </summary>
public sealed record SignalAnalysis(
    SignalClassification Signal,
    IReadOnlyList<string> ExplanationLines,
    RiskPlan Risk,
    decimal LastClose,
    MarketRegime Regime,
    CandleStatus DataStatus);

/// <summary>
/// Cycle 3 facade (FR-26): composes the existing pure stages — IndicatorEngine →
/// RegimeClassifier → ScoringEngine → SignalClassifier (+ RiskPlanCalculator) — into
/// one call that yields the current signal to display. It computes NOTHING new: it
/// only wires stages that already exist and are individually tested.
///
/// Deterministic per call (FR-24): a FRESH <see cref="SignalClassifier"/> is built
/// each invocation, so the display reflects the true classification of the supplied
/// data rather than a cooldown/dedupe state (cooldown is a notification concern for a
/// later slice). A risk plan is built only for an actionable signal with a real ATR;
/// otherwise the plan is a rejection carrying the real reason — never fabricated levels.
/// </summary>
public sealed class SignalAnalysisService
{
    private readonly IClock _clock;
    private readonly IndicatorConfig _indicator;
    private readonly ScoringConfig _scoring;
    private readonly ClassifierConfig _classifier;
    private readonly RiskPlanConfig _risk;

    public SignalAnalysisService(
        IClock clock,
        IndicatorConfig? indicator = null,
        ScoringConfig? scoring = null,
        ClassifierConfig? classifier = null,
        RiskPlanConfig? risk = null)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _indicator = indicator ?? new IndicatorConfig();
        _scoring = scoring ?? new ScoringConfig();
        _classifier = classifier ?? new ClassifierConfig();
        _risk = risk ?? new RiskPlanConfig();
    }

    /// <summary>Analyse a candle series and produce the display projection.</summary>
    public SignalAnalysis Analyze(
        NormalizedSymbol symbol,
        TimeFrame timeFrame,
        IReadOnlyList<Candle> candles,
        SymbolSpec spec,
        decimal accountBalance,
        SignalContext context,
        bool hasOpenPosition)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        if (candles is null) throw new ArgumentNullException(nameof(candles));
        if (candles.Count == 0) throw new ArgumentException("No candles to analyse.", nameof(candles));
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        context ??= new SignalContext();

        var snapshot = new IndicatorEngine(_indicator).Compute(symbol, timeFrame, candles);
        var regime = new RegimeClassifier(_scoring).Classify(snapshot);
        decimal lastClose = candles[^1].Close;
        var score = new ScoringEngine(_scoring).Score(snapshot, lastClose, regime);
        var signal = new SignalClassifier(_clock, _classifier).Classify(score, context);
        var lines = SignalExplanation.Lines(signal);

        RiskPlan risk;
        if (!signal.IsActionable)
            risk = RiskPlan.Rejected(signal.Direction, "No actionable signal — no risk plan proposed.");
        else if (snapshot.Value("ATR") is decimal atr && atr > 0m)
            risk = new RiskPlanCalculator(_risk)
                .Build(signal.Direction, lastClose, atr, spec, accountBalance, hasOpenPosition);
        else
            risk = RiskPlan.Rejected(signal.Direction, "ATR unavailable (insufficient data) — no risk plan.");

        return new SignalAnalysis(signal, lines, risk, lastClose, regime, snapshot.Status);
    }

    /// <summary>
    /// Convenience over an <see cref="IMarketDataProvider"/> — pulls the latest candles
    /// and analyses them. The provider governs liveness; Csv/Test providers are
    /// <c>IsLive=false</c>, so no fabricated "live" path exists here (INV-4).
    /// </summary>
    public async Task<SignalAnalysis> AnalyzeLatestAsync(
        IMarketDataProvider provider,
        NormalizedSymbol symbol,
        TimeFrame timeFrame,
        int count,
        SymbolSpec spec,
        decimal accountBalance,
        SignalContext context,
        bool hasOpenPosition,
        CancellationToken ct = default)
    {
        if (provider is null) throw new ArgumentNullException(nameof(provider));
        var candles = await provider.GetCandlesAsync(symbol, timeFrame, count, ct).ConfigureAwait(false);
        return Analyze(symbol, timeFrame, candles, spec, accountBalance, context, hasOpenPosition);
    }
}
