using GoldSignalAnalyzer.Application.Indicators;
using GoldSignalAnalyzer.Application.Risk;
using GoldSignalAnalyzer.Application.Scoring;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Testing;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// Phase 5 — Regime, Scoring & Veto (FR-16 … FR-25).
public class ScoringAndVetoTests
{
    private static readonly NormalizedSymbol Gold = NormalizedSymbol.Gold;
    private static readonly DateTimeOffset T0 = new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    private static IndicatorSnapshot Snap(CandleStatus st, params (string name, IndicatorCategory cat, decimal val)[] readings)
        => new(Gold, TimeFrame.H1, st,
               readings.Select(r => new IndicatorReading(r.name, r.cat, r.val, TimeFrame.H1, st)).ToList());

    // ---- FR-16: independent buy/sell scores ----

    [Fact]
    public void Buy_and_sell_scores_are_independent_not_100_minus_each_other()
    {
        // bull trend (EMA cross + MACD) but bearish momentum (RSI + Stoch), no ADX trend
        var snap = Snap(CandleStatus.Completed,
            ("EMA_FAST", IndicatorCategory.Trend, 101m),
            ("EMA_SLOW", IndicatorCategory.Trend, 100m),
            ("MACD_HIST", IndicatorCategory.Trend, 0.5m),
            ("RSI", IndicatorCategory.Momentum, 40m),
            ("STOCH_K", IndicatorCategory.Momentum, 40m));

        var score = new ScoringEngine().Score(snap, lastClose: 2000m, MarketRegime.Ranging);

        Assert.True(score.BuyScore > 0m);
        Assert.True(score.SellScore > 0m);
        Assert.NotEqual(100m - score.BuyScore, score.SellScore);          // AC-16.2
        Assert.NotEqual(100m, Math.Round(score.BuyScore + score.SellScore));
    }

    // ---- FR-17: regime classification ----

    [Theory]
    [InlineData(30, 25, 10, MarketRegime.TrendingUp)]
    [InlineData(30, 10, 25, MarketRegime.TrendingDown)]
    public void Regime_trends_by_adx_and_di(decimal adx, decimal pdi, decimal mdi, MarketRegime expected)
    {
        var snap = Snap(CandleStatus.Completed,
            ("ADX", IndicatorCategory.Trend, adx),
            ("PLUS_DI", IndicatorCategory.Trend, pdi),
            ("MINUS_DI", IndicatorCategory.Trend, mdi));
        Assert.Equal(expected, new RegimeClassifier().Classify(snap));
    }

    [Fact]
    public void Regime_volatile_vs_ranging_by_bandwidth()
    {
        var vol = Snap(CandleStatus.Completed,
            ("ADX", IndicatorCategory.Trend, 10m),
            ("BB_WIDTH", IndicatorCategory.Volatility, 100m),
            ("BB_MIDDLE", IndicatorCategory.Volatility, 2000m));
        Assert.Equal(MarketRegime.Volatile, new RegimeClassifier().Classify(vol));

        var range = Snap(CandleStatus.Completed,
            ("ADX", IndicatorCategory.Trend, 10m),
            ("BB_WIDTH", IndicatorCategory.Volatility, 10m),
            ("BB_MIDDLE", IndicatorCategory.Volatility, 2000m));
        Assert.Equal(MarketRegime.Ranging, new RegimeClassifier().Classify(range));
    }

    // ---- FR-18/FR-15: category caps are applied and visible ----

    [Fact]
    public void Trend_category_cap_is_applied_and_visible()
    {
        var snap = Snap(CandleStatus.Completed,
            ("EMA_FAST", IndicatorCategory.Trend, 101m),
            ("EMA_SLOW", IndicatorCategory.Trend, 100m),
            ("MACD_HIST", IndicatorCategory.Trend, 0.5m),
            ("ADX", IndicatorCategory.Trend, 30m),
            ("PLUS_DI", IndicatorCategory.Trend, 25m),
            ("MINUS_DI", IndicatorCategory.Trend, 10m));

        var score = new ScoringEngine().Score(snap, 2000m, MarketRegime.TrendingUp);
        decimal cappedTrendBuy = score.BuyEvidence.Where(c => c.Category == IndicatorCategory.Trend).Sum(c => c.CappedPoints);
        Assert.Equal(30m, cappedTrendBuy); // 10+10+15 raw → capped to 30

        var adx = score.Contributions.First(c => c.Indicator == "ADX_DI");
        Assert.Equal(15m, adx.RawPoints);
        Assert.Equal(10m, adx.CappedPoints); // third trend signal trimmed by the cap — visible
    }

    // ---- FR-20: hard veto forces Neutral ----

    [Fact]
    public void Veto_forces_neutral()
    {
        var score = new ScoreResult(80m, 5m, MarketRegime.TrendingUp,
            new ScoringConfig().TrendingWeights, Array.Empty<ScoreContribution>(),
            Array.Empty<Penalty>(), new[] { new Veto("SPREAD TOO WIDE") });
        var c = new SignalClassifier(new ManualClock(T0)).Classify(score, new SignalContext(HtfDirection: SignalDirection.Buy));
        Assert.Equal(SignalDirection.Neutral, c.Direction);
        Assert.Equal("SPREAD TOO WIDE", c.PrimaryReason);
    }

    [Fact]
    public void Stale_data_forces_neutral_with_exact_banner()
    {
        var score = new ScoreResult(80m, 5m, MarketRegime.TrendingUp,
            new ScoringConfig().TrendingWeights, Array.Empty<ScoreContribution>(),
            Array.Empty<Penalty>(), Array.Empty<Veto>());
        var c = new SignalClassifier(new ManualClock(T0)).Classify(score, new SignalContext(DataStale: true));
        Assert.Equal(SignalDirection.Neutral, c.Direction);
        Assert.Equal("DATA STALE — SIGNAL GENERATION PAUSED", c.PrimaryReason);
    }

    // ---- FR-21: guards ----

    private static ScoreResult ScoreOf(decimal buy, decimal sell, MarketRegime regime = MarketRegime.TrendingUp)
        => new(buy, sell, regime, new ScoringConfig().TrendingWeights,
               Array.Empty<ScoreContribution>(), Array.Empty<Penalty>(), Array.Empty<Veto>());

    [Fact]
    public void Margin_below_threshold_is_neutral()
    {
        var c = new SignalClassifier(new ManualClock(T0)).Classify(ScoreOf(50m, 40m), new SignalContext(HtfDirection: SignalDirection.Buy));
        Assert.Equal(SignalDirection.Neutral, c.Direction);
        Assert.Equal(SignalClassifier.ReasonMargin, c.PrimaryReason);
    }

    [Fact]
    public void Opposite_score_above_cap_is_neutral()
    {
        var c = new SignalClassifier(new ManualClock(T0)).Classify(ScoreOf(60m, 41m), new SignalContext(HtfDirection: SignalDirection.Buy));
        // margin 19 ≥ 15 but opposite 41 > 40 cap
        Assert.Equal(SignalDirection.Neutral, c.Direction);
        Assert.Equal(SignalClassifier.ReasonOpposite, c.PrimaryReason);
    }

    [Fact]
    public void Htf_not_confirmed_is_neutral()
    {
        var c = new SignalClassifier(new ManualClock(T0)).Classify(ScoreOf(70m, 10m), new SignalContext(HtfDirection: null));
        Assert.Equal(SignalDirection.Neutral, c.Direction);
        Assert.Equal(SignalClassifier.ReasonHtf, c.PrimaryReason);
    }

    [Fact]
    public void Cooldown_and_dedupe_suppress_repeat_then_allow_after_window()
    {
        var clock = new ManualClock(T0);
        var classifier = new SignalClassifier(clock, new ClassifierConfig { Cooldown = TimeSpan.FromMinutes(15) });
        var ctx = new SignalContext(HtfDirection: SignalDirection.Buy);

        var first = classifier.Classify(ScoreOf(70m, 10m), ctx);
        Assert.Equal(SignalDirection.Buy, first.Direction);

        clock.Advance(TimeSpan.FromMinutes(5));
        var dup = classifier.Classify(ScoreOf(70m, 10m), ctx);   // identical, within cooldown
        Assert.Equal(SignalDirection.Neutral, dup.Direction);
        Assert.Equal(SignalClassifier.ReasonCooldown, dup.PrimaryReason);

        clock.Advance(TimeSpan.FromMinutes(20));                 // beyond cooldown
        var again = classifier.Classify(ScoreOf(70m, 10m), ctx);
        Assert.Equal(SignalDirection.Buy, again.Direction);
    }

    // ---- FR-22 / FR-23 / FR-24: explanation, confidence independence, determinism ----

    [Fact]
    public void High_score_can_carry_low_confidence()
    {
        // Buy score 60 but: opposing evidence present, regime fights (TrendingDown), provisional data.
        var contribs = new List<ScoreContribution>
        {
            new("EMA_CROSS", IndicatorCategory.Trend, SignalDirection.Buy, 10, 30, 10, 1.5m, 15, "x"),
            new("MACD_HIST", IndicatorCategory.Trend, SignalDirection.Buy, 10, 30, 10, 1.5m, 15, "x"),
            new("ADX_DI",    IndicatorCategory.Trend, SignalDirection.Buy, 15, 30, 10, 1.5m, 15, "x"),
            new("RSI",       IndicatorCategory.Momentum, SignalDirection.Sell, 8, 20, 8, 1.0m, 8, "x"),
            new("STOCH",     IndicatorCategory.Momentum, SignalDirection.Sell, 8, 20, 8, 1.0m, 8, "x"),
        };
        var score = new ScoreResult(60m, 20m, MarketRegime.TrendingDown, new ScoringConfig().TrendingWeights,
            contribs, Array.Empty<Penalty>(), Array.Empty<Veto>());

        var c = new SignalClassifier(new ManualClock(T0), new ClassifierConfig { RequireHtfConfirmation = false })
            .Classify(score, new SignalContext(IsProvisional: true));

        Assert.Equal(SignalDirection.Buy, c.Direction);
        Assert.Equal(60m, c.BuyScore);
        Assert.True(c.Confidence < 30m, $"confidence={c.Confidence}"); // independent of the 60 score
    }

    [Fact]
    public void Explanation_labels_score_not_probability_and_is_deterministic()
    {
        var snap = Snap(CandleStatus.Completed,
            ("EMA_FAST", IndicatorCategory.Trend, 101m),
            ("EMA_SLOW", IndicatorCategory.Trend, 100m),
            ("MACD_HIST", IndicatorCategory.Trend, 0.5m),
            ("ADX", IndicatorCategory.Trend, 30m),
            ("PLUS_DI", IndicatorCategory.Trend, 25m),
            ("MINUS_DI", IndicatorCategory.Trend, 10m),
            ("RSI", IndicatorCategory.Momentum, 65m));
        var score = new ScoringEngine().Score(snap, 2000m, MarketRegime.TrendingUp);
        var c = new SignalClassifier(new ManualClock(T0), new ClassifierConfig { RequireHtfConfirmation = false })
            .Classify(score, new SignalContext());

        string text1 = SignalExplanation.Text(c);
        string text2 = SignalExplanation.Text(c);
        Assert.Equal(text1, text2);                             // AC-24.2 determinism
        Assert.Contains("score (0-100)", text1);                // FR-22 label
        Assert.DoesNotContain("probability", text1, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("% probability", text1);
    }

    // ---- FR-25: risk plan ----

    [Fact]
    public void Risk_plan_sizes_to_half_percent_and_1_5_min_rr()
    {
        var plan = new RiskPlanCalculator().Build(
            SignalDirection.Buy, entry: 2000m, atr: 10m, SymbolSpec.Gold(),
            accountBalance: 10_000m, hasOpenPosition: false);

        Assert.True(plan.IsTradeable);
        Assert.Equal(1985m, plan.StopLoss);   // 2000 - 1.5*10
        Assert.Equal(2030m, plan.Target);      // 2000 + 2*(1.5*10)
        Assert.Equal(2.0m, plan.RiskReward);
        Assert.Equal(0.03m, plan.PositionSizeLots); // 50 budget / (15*100)=0.0333 → floor 0.03
        Assert.True(plan.RiskAmount <= 50m);        // never exceeds the 0.5% budget
    }

    [Fact]
    public void Risk_plan_rejects_below_min_rr()
    {
        var plan = new RiskPlanCalculator(new RiskPlanConfig { TargetRewardMultiple = 1.0m })
            .Build(SignalDirection.Buy, 2000m, 10m, SymbolSpec.Gold(), 10_000m, false);
        Assert.False(plan.IsTradeable);
        Assert.Contains("R:R", plan.RejectReason);
    }

    [Fact]
    public void Risk_plan_enforces_single_open_position()
    {
        var plan = new RiskPlanCalculator().Build(
            SignalDirection.Buy, 2000m, 10m, SymbolSpec.Gold(), 10_000m, hasOpenPosition: true);
        Assert.False(plan.IsTradeable);
        Assert.Contains("only one open position", plan.RejectReason);
    }

    [Fact]
    public void Risk_plan_rejects_when_size_below_min_lot()
    {
        var plan = new RiskPlanCalculator().Build(
            SignalDirection.Buy, 2000m, 10m, SymbolSpec.Gold(), accountBalance: 100m, hasOpenPosition: false);
        Assert.False(plan.IsTradeable);
        Assert.Contains("minimum", plan.RejectReason);
    }
}
