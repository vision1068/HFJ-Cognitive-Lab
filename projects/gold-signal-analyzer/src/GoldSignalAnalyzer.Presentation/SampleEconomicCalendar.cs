using GoldSignalAnalyzer.Application.News;

namespace GoldSignalAnalyzer.Presentation;

/// <summary>
/// Cycle 10 (FR-44, D10-2): illustrative, NON-LIVE economic calendar for the sample path —
/// same "explicitly not live" honesty as <see cref="SampleCandleSeries"/>. Events are scheduled
/// relative to the supplied clock so the demo behaves sensibly regardless of when the app is
/// actually run; a real calendar API is a future seam behind <see cref="IEconomicCalendarProvider"/>.
/// </summary>
public static class SampleEconomicCalendar
{
    public static IEconomicCalendarProvider Build(DateTimeOffset now) => new StaticEconomicCalendarProvider(new[]
    {
        new EconomicEvent(now.AddDays(2), "FOMC Rate Decision (sample)", EventImpact.High),
        new EconomicEvent(now.AddDays(1), "US CPI Release (sample)", EventImpact.Medium),
        new EconomicEvent(now.AddDays(6), "US Non-Farm Payrolls (sample)", EventImpact.High),
    });
}
