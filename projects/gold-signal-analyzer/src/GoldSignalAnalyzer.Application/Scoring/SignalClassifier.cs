using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Scoring;

/// <summary>Runtime context for a classification decision (FR-20/FR-21).</summary>
public sealed record SignalContext(
    SignalDirection? HtfDirection = null,
    bool IsProvisional = false,
    bool DataStale = false);

/// <summary>FR-21 guard thresholds. All configurable.</summary>
public sealed record ClassifierConfig
{
    public decimal MinMargin { get; init; } = 15m;         // winning margin must be ≥ this
    public decimal OppositeScoreCap { get; init; } = 40m;  // opposing score above this ⇒ Neutral
    public bool RequireHtfConfirmation { get; init; } = true;
    public TimeSpan Cooldown { get; init; } = TimeSpan.FromMinutes(15);
}

/// <summary>The final, human-facing classification decision.</summary>
public sealed record SignalClassification(
    SignalDirection Direction,
    decimal Confidence,
    decimal BuyScore,
    decimal SellScore,
    MarketRegime Regime,
    string PrimaryReason,
    ScoreResult Score)
{
    public bool IsActionable => Direction != SignalDirection.Neutral;
}

/// <summary>
/// FR-20/FR-21/FR-23: applies hard vetoes and guards to the independent scores,
/// producing a final Buy/Sell/Neutral with an independently-computed confidence.
/// Guards: winning margin ≥ 15, opposing score cap, HTF confirmation, and a
/// cooldown/dedupe window (stateful, driven by an injected clock). Any veto or
/// failed guard forces Neutral (safe default). Confidence is derived from
/// evidence agreement, regime fit and data quality — NEVER from the winning
/// score value (FR-23), so a high score can carry low confidence.
/// </summary>
public sealed class SignalClassifier
{
    public const string ReasonStale = "DATA STALE — SIGNAL GENERATION PAUSED";
    public const string ReasonNoEdge = "NO DIRECTIONAL EDGE — NEUTRAL";
    public const string ReasonMargin = "WINNING MARGIN BELOW THRESHOLD — NEUTRAL";
    public const string ReasonOpposite = "OPPOSING SCORE TOO HIGH — NEUTRAL";
    public const string ReasonHtf = "HIGHER-TIMEFRAME NOT CONFIRMED — NEUTRAL";
    public const string ReasonCooldown = "COOLDOWN / DUPLICATE — SUPPRESSED";

    private readonly ClassifierConfig _cfg;
    private readonly IClock _clock;
    private (DateTimeOffset time, SignalDirection dir)? _lastEmit;

    public SignalClassifier(IClock clock, ClassifierConfig? cfg = null)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _cfg = cfg ?? new ClassifierConfig();
    }

    public SignalClassification Classify(ScoreResult score, SignalContext ctx)
    {
        if (score is null) throw new ArgumentNullException(nameof(score));

        SignalClassification Neutral(string reason) =>
            new(SignalDirection.Neutral, 0m, score.BuyScore, score.SellScore, score.Regime, reason, score);

        // FR-20 hard vetoes force Neutral regardless of scores.
        if (ctx.DataStale) return Neutral(ReasonStale);
        if (score.Vetoes.Count > 0) return Neutral(score.Vetoes[0].Reason);

        var proposed = score.BuyScore > score.SellScore ? SignalDirection.Buy
                     : score.SellScore > score.BuyScore ? SignalDirection.Sell
                     : SignalDirection.Neutral;
        if (proposed == SignalDirection.Neutral) return Neutral(ReasonNoEdge);

        decimal margin = Math.Abs(score.BuyScore - score.SellScore);
        decimal opposite = proposed == SignalDirection.Buy ? score.SellScore : score.BuyScore;

        // FR-21 guards
        if (margin < _cfg.MinMargin) return Neutral(ReasonMargin);
        if (opposite > _cfg.OppositeScoreCap) return Neutral(ReasonOpposite);
        if (_cfg.RequireHtfConfirmation && ctx.HtfDirection != proposed) return Neutral(ReasonHtf);

        // Cooldown + dedupe: an identical direction within the window is suppressed.
        if (_lastEmit is { } last && last.dir == proposed && (_clock.UtcNow - last.time) < _cfg.Cooldown)
            return Neutral(ReasonCooldown);

        decimal confidence = ComputeConfidence(proposed, score, ctx);
        _lastEmit = (_clock.UtcNow, proposed);

        string reason = proposed == SignalDirection.Buy
            ? $"BUY — score {score.BuyScore:0.#} vs {score.SellScore:0.#}, margin {margin:0.#}"
            : $"SELL — score {score.SellScore:0.#} vs {score.BuyScore:0.#}, margin {margin:0.#}";

        return new SignalClassification(proposed, confidence, score.BuyScore, score.SellScore, score.Regime, reason, score);
    }

    /// <summary>FR-23: confidence independent of the winning score value.</summary>
    private static decimal ComputeConfidence(SignalDirection dir, ScoreResult score, SignalContext ctx)
    {
        int lead = score.Contributions.Count(c => c.Direction == dir);
        int opp = score.Contributions.Count(c => c.Direction != dir && c.Direction != SignalDirection.Neutral);
        int total = lead + opp;
        decimal agreement = total == 0 ? 0.5m : (decimal)lead / total;

        decimal conf = 50m + (agreement - 0.5m) * 80m; // evidence agreement dominates

        // regime fit
        bool fits = (dir == SignalDirection.Buy && score.Regime == MarketRegime.TrendingUp)
                 || (dir == SignalDirection.Sell && score.Regime == MarketRegime.TrendingDown);
        bool fights = (dir == SignalDirection.Buy && score.Regime == MarketRegime.TrendingDown)
                   || (dir == SignalDirection.Sell && score.Regime == MarketRegime.TrendingUp);
        if (fits) conf += 15m;
        else if (fights) conf -= 15m;

        // data quality
        if (ctx.IsProvisional) conf -= 20m;

        return Math.Clamp(Math.Round(conf, 1), 0m, 100m);
    }
}
