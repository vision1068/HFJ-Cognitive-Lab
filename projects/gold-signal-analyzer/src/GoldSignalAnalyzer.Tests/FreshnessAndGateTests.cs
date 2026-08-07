using GoldSignalAnalyzer.Application.Freshness;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Testing;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// FR-12: data-freshness tracking + veto / "DATA STALE — SIGNAL GENERATION PAUSED".
public class FreshnessAndGateTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Unknown_before_any_tick()
    {
        var clock = new ManualClock(T0);
        var monitor = new DataFreshnessMonitor(clock, TimeSpan.FromSeconds(5));
        Assert.Equal(FreshnessStatus.Unknown, monitor.Assess().Status);
    }

    [Fact]
    public void Fresh_within_threshold_then_stale_after()
    {
        var clock = new ManualClock(T0);
        var monitor = new DataFreshnessMonitor(clock, TimeSpan.FromSeconds(5));
        monitor.RecordTick(T0);

        clock.Advance(TimeSpan.FromSeconds(4));
        Assert.Equal(FreshnessStatus.Fresh, monitor.Assess().Status);

        clock.Advance(TimeSpan.FromSeconds(2)); // now 6s old > 5s
        Assert.Equal(FreshnessStatus.Stale, monitor.Assess().Status);
    }

    [Fact]
    public void Future_dated_tick_is_treated_as_stale_not_fresh()
    {
        var clock = new ManualClock(T0);
        var monitor = new DataFreshnessMonitor(clock, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(2));
        monitor.RecordTick(T0.AddSeconds(10)); // 10s in the future
        Assert.Equal(FreshnessStatus.Stale, monitor.Assess().Status);
    }

    [Fact]
    public void Reset_returns_to_unknown()
    {
        var clock = new ManualClock(T0);
        var monitor = new DataFreshnessMonitor(clock, TimeSpan.FromSeconds(5));
        monitor.RecordTick(T0);
        monitor.Reset();
        Assert.Equal(FreshnessStatus.Unknown, monitor.Assess().Status);
    }

    // ---- SignalGate veto ----

    private static readonly SignalGate Gate = new();
    private static FreshnessAssessment Fresh => new(FreshnessStatus.Fresh, TimeSpan.Zero, T0);
    private static FreshnessAssessment Stale => new(FreshnessStatus.Stale, TimeSpan.FromMinutes(1), T0);

    [Fact]
    public void Gate_allows_only_when_connected_fresh_and_mapped()
    {
        var d = Gate.Evaluate(ConnectionState.Connected, Fresh, hasSymbolMapping: true);
        Assert.True(d.IsAllowed);
        Assert.Null(d.Reason);
    }

    [Fact]
    public void Gate_suppresses_on_stale_with_exact_banner()
    {
        var d = Gate.Evaluate(ConnectionState.Connected, Stale, hasSymbolMapping: true);
        Assert.False(d.IsAllowed);
        Assert.Equal("DATA STALE — SIGNAL GENERATION PAUSED", d.Reason);
    }

    [Theory]
    [InlineData(ConnectionState.Disconnected)]
    [InlineData(ConnectionState.Reconnecting)]
    [InlineData(ConnectionState.Faulted)]
    public void Gate_suppresses_while_not_connected(ConnectionState state)
    {
        var d = Gate.Evaluate(state, Fresh, hasSymbolMapping: true);
        Assert.False(d.IsAllowed);
        Assert.Equal(SignalGate.NotConnectedReason, d.Reason);
    }

    [Fact]
    public void Gate_suppresses_when_no_symbol_mapped()
    {
        var d = Gate.Evaluate(ConnectionState.Connected, Fresh, hasSymbolMapping: false);
        Assert.False(d.IsAllowed);
        Assert.Equal(SignalGate.NoSymbolReason, d.Reason);
    }

    [Fact]
    public void Gate_suppresses_when_freshness_unknown()
    {
        var unknown = new FreshnessAssessment(FreshnessStatus.Unknown, TimeSpan.Zero, null);
        var d = Gate.Evaluate(ConnectionState.Connected, unknown, hasSymbolMapping: true);
        Assert.False(d.IsAllowed);
        Assert.Equal(SignalGate.StaleReason, d.Reason);
    }
}
