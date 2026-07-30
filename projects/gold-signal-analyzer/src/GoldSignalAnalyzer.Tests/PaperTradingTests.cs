using System.Reflection;
using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.PaperTrading;
using GoldSignalAnalyzer.Application.Risk;
using GoldSignalAnalyzer.Domain;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// Phase 9 — Paper Trading (FR-32, FR-33).
public class PaperTradingTests
{
    private static readonly SymbolSpec Spec = SymbolSpec.Gold();
    private static readonly DateTimeOffset T0 = new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    private static RiskPlan TradeablePlan() =>
        new RiskPlanCalculator().Build(SignalDirection.Buy, 2000m, 10m, Spec, 10_000m, hasOpenPosition: false);

    // ---- AC-32.2: journal captures the full context ----

    [Fact]
    public void Open_records_full_journal_entry()
    {
        var store = new InMemoryJournalStore();
        var broker = new PaperBroker(store, Spec);
        var r = broker.Open(TradeablePlan(), T0, MarketRegime.TrendingUp, confidence: 72m, entryReason: "BUY breakout");

        Assert.True(r.Ok);
        var e = Assert.Single(store.All());
        Assert.Equal(SignalDirection.Buy, e.Direction);
        Assert.Equal(PaperTradeStatus.Open, e.Status);
        Assert.Equal(2000m, e.EntryPrice);
        Assert.Equal(1985m, e.StopLoss);
        Assert.Equal(2030m, e.Target);
        Assert.Equal(0.03m, e.SizeLots);
        Assert.Equal(2.0m, e.RiskReward);
        Assert.Equal(MarketRegime.TrendingUp, e.Regime);
        Assert.Equal(72m, e.Confidence);
        Assert.Equal("BUY breakout", e.EntryReason);
    }

    // ---- AC-32.1: simulated fill P&L from candle price ----

    [Fact]
    public void Close_computes_pnl_from_exit_price()
    {
        var store = new InMemoryJournalStore();
        var broker = new PaperBroker(store, Spec);
        broker.Open(TradeablePlan(), T0, MarketRegime.TrendingUp, 72m, "entry");

        var r = broker.Close(exitPrice: 2010m, T0.AddHours(3), "target hit");
        Assert.True(r.Ok);
        // (2010-2000) * +1 * contractSize 100 * 0.03 lots = 30
        Assert.Equal(30m, r.Entry!.NetPnl);
        Assert.Equal(PaperTradeStatus.Closed, r.Entry.Status);
        Assert.Null(store.GetOpen());
    }

    // ---- AC-32.3: single open position enforced ----

    [Fact]
    public void Second_open_while_one_is_open_is_rejected_then_allowed_after_close()
    {
        var store = new InMemoryJournalStore();
        var broker = new PaperBroker(store, Spec);
        Assert.True(broker.Open(TradeablePlan(), T0, MarketRegime.TrendingUp, 72m, "first").Ok);

        var second = broker.Open(TradeablePlan(), T0.AddMinutes(1), MarketRegime.TrendingUp, 72m, "second");
        Assert.False(second.Ok);
        Assert.Contains("only one open position", second.Message);

        broker.Close(2010m, T0.AddHours(1), "exit");
        Assert.True(broker.Open(TradeablePlan(), T0.AddHours(2), MarketRegime.TrendingUp, 72m, "third").Ok);
    }

    // ---- FR-33: structurally incapable of a live order ----

    [Fact]
    public void Bridge_client_exposes_no_order_or_trade_surface()
    {
        string[] forbidden = { "order", "trade", "buy", "sell", "submit", "execute", "modify", "deal", "position", "sendorder" };
        var methodNames = typeof(IMt5BridgeClient)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name.ToLowerInvariant());
        foreach (var name in methodNames)
            Assert.DoesNotContain(forbidden, bad => name.Contains(bad));
    }

    [Fact]
    public void PaperBroker_has_no_reference_to_the_bridge_client()
    {
        var t = typeof(PaperBroker);
        // no field, property, or constructor parameter is an IMt5BridgeClient
        Assert.DoesNotContain(t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance),
            f => typeof(IMt5BridgeClient).IsAssignableFrom(f.FieldType));
        Assert.DoesNotContain(t.GetConstructors().SelectMany(c => c.GetParameters()),
            p => typeof(IMt5BridgeClient).IsAssignableFrom(p.ParameterType));
    }
}
