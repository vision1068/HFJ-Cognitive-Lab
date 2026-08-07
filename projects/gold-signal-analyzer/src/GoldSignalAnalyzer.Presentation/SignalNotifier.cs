using System.Collections.ObjectModel;
using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Analysis;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Presentation.Mvvm;

namespace GoldSignalAnalyzer.Presentation;

/// <summary>
/// FR-30: raises an in-app notification when a NEW actionable (Buy/Sell) signal is
/// classified, deduplicated by direction within a cooldown window (mirroring the
/// <c>SignalClassifier</c> cooldown concept, driven by an injected <see cref="IClock"/>
/// so it is testable with a ManualClock). A Neutral/vetoed classification raises
/// nothing and does not disturb the dedupe state (AC-30.2/AC-30.5).
///
/// It is a PASSIVE surface (INV-1): it exposes an ordered notification collection and
/// an unread count — and NO trade/execute/dismiss-to-order command. Nothing here can
/// place, modify or simulate an order.
/// </summary>
public sealed class SignalNotifier : ViewModelBase
{
    private readonly IClock _clock;
    private readonly TimeSpan _cooldown;
    private (SignalDirection dir, DateTimeOffset time)? _lastAlert;
    private int _unread;

    /// <param name="cooldown">Suppress an identical direction inside this window.
    /// Defaults to 15 min, matching <c>ClassifierConfig.Cooldown</c>.</param>
    public SignalNotifier(IClock clock, TimeSpan? cooldown = null)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _cooldown = cooldown ?? TimeSpan.FromMinutes(15);
    }

    /// <summary>Newest-first (AC-30.4).</summary>
    public ObservableCollection<NotificationViewModel> Notifications { get; } = new();

    public int UnreadCount
    {
        get => _unread;
        private set => SetField(ref _unread, value);
    }

    public bool HasNotifications => Notifications.Count > 0;

    public NotificationViewModel? Latest => Notifications.Count > 0 ? Notifications[0] : null;

    /// <summary>Mark all as read (a pure display action — not a trade action).</summary>
    public void MarkAllRead() => UnreadCount = 0;

    /// <summary>
    /// Feed the current analysis. Returns the raised notification, or null if nothing
    /// was raised (not actionable, or a duplicate inside the cooldown window).
    /// </summary>
    public NotificationViewModel? Observe(SignalAnalysis analysis)
    {
        if (analysis is null) throw new ArgumentNullException(nameof(analysis));

        var dir = analysis.Signal.Direction;
        if (dir == SignalDirection.Neutral)
            return null;   // AC-30.2/30.5: Neutral/vetoed raises nothing, state untouched

        var now = _clock.UtcNow;
        if (_lastAlert is { } last && last.dir == dir && (now - last.time) < _cooldown)
            return null;   // AC-30.2: same direction inside cooldown → deduped

        var note = new NotificationViewModel(now, analysis);
        Notifications.Insert(0, note);            // newest first
        _lastAlert = (dir, now);
        UnreadCount++;
        OnPropertyChanged(nameof(HasNotifications));
        OnPropertyChanged(nameof(Latest));
        return note;
    }
}
