namespace GoldSignalAnalyzer.Application.News;

/// <summary>Cycle 10 (FR-44): how strongly a scheduled economic release typically moves gold.
/// Only <see cref="High"/> ever triggers a blackout (D10-3).</summary>
public enum EventImpact
{
    Low = 0,
    Medium = 1,
    High = 2
}

/// <summary>A single scheduled economic-calendar entry. A fact with a timestamp — never a
/// headline, an opinion, or a sentiment score (D10-1).</summary>
public sealed record EconomicEvent(DateTimeOffset TimeUtc, string Name, EventImpact Impact);

/// <summary>
/// Cycle 10 (D10-2): the calendar seam. Mirrors the read-only market-data-provider pattern from
/// Cycle 1 — a real calendar API can implement this later without touching
/// <see cref="NewsBlackoutGate"/> or <see cref="Scoring.SignalClassifier"/>.
/// </summary>
public interface IEconomicCalendarProvider
{
    IReadOnlyList<EconomicEvent> GetEvents();
}

/// <summary>
/// Cycle 10 (D10-2): the only implementation this cycle — a fixed, in-memory event list. NOT a
/// live feed; carries no API key/secret (INV-2/INV-3).
/// </summary>
public sealed class StaticEconomicCalendarProvider : IEconomicCalendarProvider
{
    private readonly IReadOnlyList<EconomicEvent> _events;

    public StaticEconomicCalendarProvider(IReadOnlyList<EconomicEvent>? events = null)
        => _events = events ?? Array.Empty<EconomicEvent>();

    public IReadOnlyList<EconomicEvent> GetEvents() => _events;
}

/// <summary>
/// FR-44: a deterministic, calendar-only news-blackout check. No sentiment, no interpretation,
/// no fabricated data (INV-4) — purely "is now inside the window of a High-impact scheduled
/// event." Fail-open when the calendar is empty (D10-4a): an unpopulated calendar must never
/// silently freeze the app. Pure function of its inputs (NFR-14).
/// </summary>
public sealed class NewsBlackoutGate
{
    private readonly IEconomicCalendarProvider _calendar;
    private readonly TimeSpan _window;

    public NewsBlackoutGate(IEconomicCalendarProvider calendar, TimeSpan? window = null)
    {
        _calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        _window = window ?? TimeSpan.FromMinutes(30);
    }

    /// <summary>The active High-impact event if <paramref name="now"/> falls within the blackout
    /// window of one, else <c>null</c>. Medium/Low-impact events never trigger a blackout.</summary>
    public EconomicEvent? ActiveBlackout(DateTimeOffset now)
    {
        foreach (var e in _calendar.GetEvents())
        {
            if (e.Impact != EventImpact.High) continue;
            if (now >= e.TimeUtc - _window && now <= e.TimeUtc + _window) return e;
        }
        return null;
    }
}
