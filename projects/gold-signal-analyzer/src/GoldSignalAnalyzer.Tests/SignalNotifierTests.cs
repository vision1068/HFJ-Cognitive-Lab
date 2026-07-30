using GoldSignalAnalyzer.Application.Analysis;
using GoldSignalAnalyzer.Application.Risk;
using GoldSignalAnalyzer.Application.Scoring;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Presentation;
using GoldSignalAnalyzer.Testing;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// Cycle 4 (FR-30) — the in-app signal notifier. Alerts only on a NEW actionable
/// signal, deduped by direction inside the cooldown window; Neutral raises nothing;
/// the surface is passive (no trade/execute command). Driven by a ManualClock.
/// </summary>
public class SignalNotifierTests
{
    private static readonly DateTimeOffset T0 = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    private static SignalAnalysis Make(SignalDirection dir, string? reason = null)
    {
        decimal buy = dir == SignalDirection.Buy ? 72m : 18m;
        decimal sell = dir == SignalDirection.Sell ? 72m : 18m;
        var score = new ScoreResult(buy, sell, MarketRegime.TrendingUp,
            new CategoryWeights(1m, 1m, 1m, 1m),
            Array.Empty<ScoreContribution>(), Array.Empty<Penalty>(), Array.Empty<Veto>());
        var sig = new SignalClassification(
            dir,
            dir == SignalDirection.Neutral ? 0m : 63m,
            buy, sell, MarketRegime.TrendingUp,
            reason ?? (dir == SignalDirection.Neutral ? SignalClassifier.ReasonNoEdge : $"{dir} — actionable"),
            score);
        return new SignalAnalysis(sig, Array.Empty<string>(),
            RiskPlan.Rejected(dir, "test — no plan"), 1900m, MarketRegime.TrendingUp, CandleStatus.Completed);
    }

    [Fact] // AC-30.1: first actionable signal raises exactly one notification
    public void First_actionable_signal_raises_notification()
    {
        var clock = new ManualClock(T0);
        var n = new SignalNotifier(clock);

        var note = n.Observe(Make(SignalDirection.Buy));

        Assert.NotNull(note);
        Assert.Equal("BUY", note!.Direction);
        Assert.Single(n.Notifications);
        Assert.Equal(1, n.UnreadCount);
        Assert.True(n.HasNotifications);
        Assert.Same(note, n.Latest);
    }

    [Fact] // AC-30.2: same direction inside the cooldown window is deduped
    public void Duplicate_direction_within_cooldown_is_suppressed()
    {
        var clock = new ManualClock(T0);
        var n = new SignalNotifier(clock, TimeSpan.FromMinutes(15));

        Assert.NotNull(n.Observe(Make(SignalDirection.Buy)));
        clock.Advance(TimeSpan.FromMinutes(5));
        Assert.Null(n.Observe(Make(SignalDirection.Buy)));   // still within 15-min cooldown
        Assert.Single(n.Notifications);
        Assert.Equal(1, n.UnreadCount);
    }

    [Fact] // AC-30.2: same direction AFTER the cooldown raises again
    public void Same_direction_after_cooldown_raises_again()
    {
        var clock = new ManualClock(T0);
        var n = new SignalNotifier(clock, TimeSpan.FromMinutes(15));

        n.Observe(Make(SignalDirection.Buy));
        clock.Advance(TimeSpan.FromMinutes(16));
        Assert.NotNull(n.Observe(Make(SignalDirection.Buy)));
        Assert.Equal(2, n.Notifications.Count);
    }

    [Fact] // AC-30.2: the opposite direction raises immediately (a genuinely new signal)
    public void Opposite_direction_raises_immediately()
    {
        var clock = new ManualClock(T0);
        var n = new SignalNotifier(clock);

        n.Observe(Make(SignalDirection.Buy));
        var sell = n.Observe(Make(SignalDirection.Sell));   // same instant, opposite dir
        Assert.NotNull(sell);
        Assert.Equal("SELL", sell!.Direction);
        Assert.Equal(2, n.Notifications.Count);
        Assert.Same(sell, n.Notifications[0]);              // AC-30.4: newest first
    }

    [Fact] // AC-30.2/30.5: Neutral raises nothing AND does not disturb dedupe state
    public void Neutral_raises_nothing_and_leaves_dedupe_state_intact()
    {
        var clock = new ManualClock(T0);
        var n = new SignalNotifier(clock, TimeSpan.FromMinutes(15));

        n.Observe(Make(SignalDirection.Buy));               // alert at T0
        clock.Advance(TimeSpan.FromMinutes(5));
        Assert.Null(n.Observe(Make(SignalDirection.Neutral))); // no alert
        clock.Advance(TimeSpan.FromMinutes(5));
        // Buy is still inside the ORIGINAL cooldown (10 min elapsed) — Neutral didn't reset it.
        Assert.Null(n.Observe(Make(SignalDirection.Buy)));
        Assert.Single(n.Notifications);
    }

    [Fact] // AC-30.5: a stale (vetoed→Neutral) classification produces no alert
    public void Stale_data_neutral_produces_no_alert()
    {
        var n = new SignalNotifier(new ManualClock(T0));
        Assert.Null(n.Observe(Make(SignalDirection.Neutral, SignalClassifier.ReasonStale)));
        Assert.Empty(n.Notifications);
    }

    [Fact] // AC-30.3: FR-22 — scores are labelled "score (0-100)", never a percentage
    public void Notification_labels_scores_as_0_100_not_percent()
    {
        var n = new SignalNotifier(new ManualClock(T0));
        var note = n.Observe(Make(SignalDirection.Buy))!;
        foreach (var label in new[] { note.BuyScoreLabel, note.SellScoreLabel, note.ConfidenceLabel })
        {
            Assert.Contains("(0-100", label);
            Assert.DoesNotContain("%", label);
            Assert.DoesNotContain("probability", label, StringComparison.OrdinalIgnoreCase);
        }
        Assert.Contains("actionable", note.Reason);
    }

    [Fact] // MarkAllRead clears the unread badge without removing history
    public void MarkAllRead_clears_unread_but_keeps_history()
    {
        var n = new SignalNotifier(new ManualClock(T0));
        n.Observe(Make(SignalDirection.Buy));
        Assert.Equal(1, n.UnreadCount);
        n.MarkAllRead();
        Assert.Equal(0, n.UnreadCount);
        Assert.Single(n.Notifications);
    }

    [Fact] // AC-30.4/INV-1: the notifier exposes NO trade/execution command
    public void Notifier_exposes_no_order_or_execution_command()
    {
        var members = typeof(SignalNotifier).GetMembers().Select(m => m.Name.ToLowerInvariant()).ToList();
        foreach (var forbidden in new[] { "open", "close", "buy", "sell", "execute", "submit", "order", "trade", "place" })
            Assert.DoesNotContain(members, m => m.Contains(forbidden));
    }
}
