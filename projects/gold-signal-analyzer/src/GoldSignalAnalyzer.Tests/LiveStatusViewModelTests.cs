using GoldSignalAnalyzer.Application.Freshness;
using GoldSignalAnalyzer.Application.Live;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Presentation;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// Cycle 7 (FR-37) — the live status strip. A suppressed refresh must surface the
/// EXACT veto banner and mark IsSuppressed, so a paused feed can never read as an
/// actionable signal (INV-4). It is passive: no command, no order affordance (INV-1).
/// </summary>
public class LiveStatusViewModelTests
{
    private static LiveRefreshResult Suppressed(string reason, ConnectionState state) => new(
        new SignalGateDecision(SignalGateStatus.Suppressed, reason),
        Analysis: null,
        Candles: Array.Empty<Candle>(),
        Freshness: new FreshnessAssessment(FreshnessStatus.Unknown, TimeSpan.Zero, null),
        ConnectionState: state);

    [Fact] // A disconnected/suppressed refresh surfaces the exact banner and flags suppression.
    public void Suppressed_refresh_shows_exact_banner()
    {
        var vm = new LiveStatusViewModel();
        vm.Update(Suppressed(SignalGate.NotConnectedReason, ConnectionState.Disconnected));

        Assert.True(vm.IsLive);
        Assert.True(vm.IsSuppressed);
        Assert.Equal(SignalGate.NotConnectedReason, vm.Banner);
    }

    [Fact] // Stale data surfaces the FR-12 DATA STALE banner verbatim.
    public void Stale_refresh_shows_data_stale_banner()
    {
        var vm = new LiveStatusViewModel();
        vm.Update(Suppressed(SignalGate.StaleReason, ConnectionState.Connected));
        Assert.True(vm.IsSuppressed);
        Assert.Equal(SignalGate.StaleReason, vm.Banner);
    }

    [Fact] // MarkLive flips IsLive before the first refresh, without claiming "connected".
    public void MarkLive_sets_live_without_claiming_fresh()
    {
        var vm = new LiveStatusViewModel();
        Assert.False(vm.IsLive);
        vm.MarkLive();
        Assert.True(vm.IsLive);
        Assert.False(vm.IsSuppressed); // not yet suppressed, but also not "fresh"
    }

    [Fact] // INV-1: the status strip exposes NO command / order affordance.
    public void Status_strip_exposes_no_order_or_command_member()
    {
        var members = typeof(LiveStatusViewModel).GetMembers().Select(m => m.Name.ToLowerInvariant()).ToList();
        foreach (var forbidden in new[] { "command", "open", "close", "buy", "sell", "execute", "submit", "order", "trade", "place" })
            Assert.DoesNotContain(members, m => m.Contains(forbidden));
    }
}
