using System.Globalization;
using GoldSignalAnalyzer.Application.Analysis;
using GoldSignalAnalyzer.Application.PaperTrading;
using GoldSignalAnalyzer.Application.Risk;
using GoldSignalAnalyzer.Application.Scoring;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Presentation;
using GoldSignalAnalyzer.Testing;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// Cycle 3 (UI/dashboard shell) — headless tests for the pure presentation logic and
/// the SignalAnalysisService facade. The WPF project carries no logic, so everything
/// verifiable is exercised here without a display.
/// </summary>
public class UiShellTests
{
    // ---- shared fixtures ---------------------------------------------------

    private static List<Candle> RisingSeries(int n = 80)
    {
        var list = new List<Candle>();
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (int i = 0; i < n; i++)
        {
            decimal open = 1900m + i * 1.5m;
            decimal close = open + 1.0m;
            list.Add(new Candle(NormalizedSymbol.Gold, TimeFrame.H1, start.AddHours(i),
                open, close + 0.5m, open - 0.5m, close, 1000 + i));
        }
        return list;
    }

    private static SignalAnalysis AnalyzeRising(SignalDirection? htf = SignalDirection.Buy)
    {
        var svc = new SignalAnalysisService(new ManualClock(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero)));
        return svc.Analyze(NormalizedSymbol.Gold, TimeFrame.H1, RisingSeries(),
            SymbolSpec.Gold(), 10_000m, new SignalContext(HtfDirection: htf), hasOpenPosition: false);
    }

    // ---- SignalAnalysisService facade (FR-26) ------------------------------

    [Fact] // AC-26.1: a rising series with HTF=Buy yields an actionable BUY
    public void Facade_rising_series_produces_actionable_buy()
    {
        var a = AnalyzeRising();
        Assert.Equal(SignalDirection.Buy, a.Signal.Direction);
        Assert.True(a.Signal.IsActionable);
        Assert.True(a.Signal.BuyScore > a.Signal.SellScore);
        Assert.NotEmpty(a.ExplanationLines);
    }

    [Fact] // AC-26.4: an actionable signal with real ATR yields a tradeable risk plan
    public void Facade_actionable_signal_has_tradeable_risk_plan()
    {
        var a = AnalyzeRising();
        Assert.True(a.Risk.IsTradeable);
        Assert.True(a.Risk.RiskReward >= 1.5m);
        Assert.Equal(SignalDirection.Buy, a.Risk.Direction);
    }

    [Fact] // AC-26.3: stale data vetoes to Neutral with the exact banner reason
    public void Facade_stale_data_forces_neutral_with_exact_banner()
    {
        var svc = new SignalAnalysisService(new ManualClock(DateTimeOffset.UtcNow));
        var a = svc.Analyze(NormalizedSymbol.Gold, TimeFrame.H1, RisingSeries(),
            SymbolSpec.Gold(), 10_000m, new SignalContext(DataStale: true), hasOpenPosition: false);

        Assert.Equal(SignalDirection.Neutral, a.Signal.Direction);
        Assert.False(a.Signal.IsActionable);
        Assert.Equal("DATA STALE — SIGNAL GENERATION PAUSED", a.Signal.PrimaryReason);
        Assert.False(a.Risk.IsTradeable); // AC-26.4: no fabricated plan for Neutral
    }

    [Fact]
    public void Facade_rejects_empty_candles()
        => Assert.Throws<ArgumentException>(() => new SignalAnalysisService(new ManualClock(DateTimeOffset.UtcNow))
            .Analyze(NormalizedSymbol.Gold, TimeFrame.H1, new List<Candle>(),
                SymbolSpec.Gold(), 10_000m, new SignalContext(), false));

    // ---- SignalViewModel (FR-26 / FR-22) -----------------------------------

    [Fact] // AC-26.1
    public void SignalVm_exposes_direction_scores_and_explanation()
    {
        var vm = new SignalViewModel();
        vm.Load(AnalyzeRising());
        Assert.True(vm.HasSignal);
        Assert.Equal("BUY", vm.Direction);
        Assert.True(vm.IsActionable);
        Assert.StartsWith("Buy score (0-100):", vm.BuyScoreLabel);
        Assert.StartsWith("Sell score (0-100):", vm.SellScoreLabel);
        Assert.NotEmpty(vm.ExplanationLines);
    }

    [Fact] // AC-26.2: FR-22 — scores are never rendered as a percentage/probability
    public void SignalVm_never_formats_score_as_percent_or_probability()
    {
        var vm = new SignalViewModel();
        vm.Load(AnalyzeRising());
        foreach (var label in new[] { vm.BuyScoreLabel, vm.SellScoreLabel, vm.ConfidenceLabel })
        {
            Assert.DoesNotContain("%", label);
            Assert.DoesNotContain("probability", label, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("(0-100", label);
        }
    }

    [Fact] // AC-26.4: a Neutral signal shows no fabricated risk levels, only the reason
    public void SignalVm_neutral_shows_reject_reason_not_levels()
    {
        var svc = new SignalAnalysisService(new ManualClock(DateTimeOffset.UtcNow));
        var a = svc.Analyze(NormalizedSymbol.Gold, TimeFrame.H1, RisingSeries(),
            SymbolSpec.Gold(), 10_000m, new SignalContext(DataStale: true), false);
        var vm = new SignalViewModel();
        vm.Load(a);

        Assert.False(vm.HasRiskPlan);
        Assert.StartsWith("No risk plan:", vm.RiskPlanSummary);
    }

    [Fact]
    public void SignalVm_empty_state_is_safe_placeholder()
    {
        var vm = new SignalViewModel();
        Assert.False(vm.HasSignal);
        Assert.Equal("—", vm.Direction);
        Assert.Empty(vm.ExplanationLines);
        Assert.Equal("—", vm.RiskPlanSummary);
    }

    // ---- JournalViewModel (FR-26.2) ----------------------------------------

    private static JournalEntry ClosedTrade(decimal net, DateTimeOffset t)
        => new()
        {
            Direction = SignalDirection.Buy,
            Status = PaperTradeStatus.Closed,
            EntryTimeUtc = t, EntryPrice = 1900m, StopLoss = 1890m, Target = 1930m,
            SizeLots = 0.1m, RiskReward = 3m, Regime = MarketRegime.TrendingUp, Confidence = 60m,
            EntryReason = "test", ExitTimeUtc = t.AddHours(2), ExitPrice = 1930m,
            ExitReason = "target", GrossPnl = net, NetPnl = net,
        };

    [Fact] // AC-26.2a/AC-26.2d: rows load; realised total sums CLOSED net P&L only
    public void JournalVm_loads_rows_and_totals_closed_pnl_only()
    {
        var store = new InMemoryJournalStore();
        var t = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        store.Add(ClosedTrade(100m, t));
        store.Add(ClosedTrade(-40m, t.AddHours(3)));
        // an OPEN trade must NOT count toward realised total (AC-26.2c/d)
        store.Add(new JournalEntry
        {
            Direction = SignalDirection.Sell, Status = PaperTradeStatus.Open,
            EntryTimeUtc = t.AddHours(6), EntryPrice = 1950m, StopLoss = 1960m, Target = 1920m,
            SizeLots = 0.1m, RiskReward = 3m, Regime = MarketRegime.Ranging, Confidence = 55m,
            EntryReason = "open",
        });

        var vm = new JournalViewModel(store);
        Assert.Equal(3, vm.Rows.Count);
        Assert.Equal(2, vm.ClosedCount);
        Assert.Equal(1, vm.OpenCount);
        Assert.Contains("60", vm.RealisedNetPnlLabel); // 100 + (-40) = 60
    }

    [Fact] // AC-26.2c: an open trade renders blank exit/P&L, not a guessed value
    public void JournalRow_open_trade_has_blank_exit_and_pnl()
    {
        var row = new JournalRowViewModel(new JournalEntry
        {
            Direction = SignalDirection.Buy, Status = PaperTradeStatus.Open,
            EntryTimeUtc = DateTimeOffset.UtcNow, EntryPrice = 1900m, StopLoss = 1890m,
            Target = 1930m, SizeLots = 0.1m, RiskReward = 3m, Regime = MarketRegime.TrendingUp,
            Confidence = 60m, EntryReason = "x",
        });
        Assert.Equal("", row.ExitTime);
        Assert.Equal("", row.ExitPrice);
        Assert.Equal("", row.NetPnl);
    }

    [Fact] // AC-26.2b: the journal view exposes NO trade/execution command (INV-1)
    public void JournalVm_exposes_no_order_or_mutation_command()
    {
        var members = typeof(JournalViewModel).GetMembers()
            .Select(m => m.Name.ToLowerInvariant()).ToList();
        foreach (var forbidden in new[] { "open", "close", "buy", "sell", "execute", "submit", "order", "trade", "place" })
            Assert.DoesNotContain(members, n => n.Contains(forbidden) && n.Contains("command"));
    }

    // ---- Disclaimer: text, gate, persistence (FR-35 / C-1) -----------------

    [Fact] // AC-35.1: the single wording source carries the required regulated-advice phrases
    public void DisclaimerText_contains_required_regulated_advice_phrases()
    {
        var full = DisclaimerText.Full.ToLowerInvariant();
        Assert.Contains("not investment advice", full);
        Assert.Contains("educational", full);
        Assert.Contains("risk of loss", full);
        Assert.Contains("past performance", full);
        Assert.Contains("simulated", full);
        Assert.Contains("paper", full);
        Assert.Contains("solely responsible", full);
        // FR-22: scores described as 0-100, not probabilities
        Assert.Contains("0-100", full);
        Assert.DoesNotContain("guarantee of profit", full);
    }

    [Fact] // AC-35.2a: banner is non-empty and carries the key phrase (FR-35.2)
    public void Banner_is_non_empty_and_carries_key_phrase()
    {
        Assert.False(string.IsNullOrWhiteSpace(DisclaimerText.Banner));
        Assert.Contains("NOT INVESTMENT ADVICE", DisclaimerText.Banner);
        Assert.Contains(DisclaimerText.Banner, new MainViewModel(new SignalViewModel(),
            new JournalViewModel(new InMemoryJournalStore()), new ChartViewModel(),
            new SignalNotifier(new ManualClock(DateTimeOffset.UtcNow))).DisclaimerBanner);
    }

    [Fact] // AC-35.2: the gate starts un-acknowledged and only clears after the command
    public void Disclaimer_gate_blocks_until_acknowledged()
    {
        var store = new InMemoryAcknowledgementStore();
        var vm = new DisclaimerViewModel(store);
        bool raised = false;
        vm.Acknowledged += (_, _) => raised = true;

        Assert.True(vm.MustPrompt);
        Assert.False(vm.HasAcknowledged);

        vm.AcknowledgeCommand.Execute(null);

        Assert.True(vm.HasAcknowledged);
        Assert.False(vm.MustPrompt);
        Assert.True(raised);
    }

    [Fact] // AC-35.3: acknowledgement persists across a FRESH store instance (genuine first-run)
    public void Disclaimer_acknowledgement_persists_across_fresh_store()
    {
        var path = Path.Combine(Path.GetTempPath(), "gsa-ack-" + Guid.NewGuid().ToString("N") + ".ack");
        try
        {
            var first = new DisclaimerViewModel(new FileAcknowledgementStore(path));
            Assert.True(first.MustPrompt);
            first.AcknowledgeCommand.Execute(null);
            Assert.True(first.HasAcknowledged);

            // A brand-new store on the same path must observe the acknowledgement.
            var second = new DisclaimerViewModel(new FileAcknowledgementStore(path));
            Assert.False(second.MustPrompt);
            Assert.True(second.HasAcknowledged);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
