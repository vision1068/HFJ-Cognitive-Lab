using GoldSignalAnalyzer.Application.News;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// Cycle 10 (FR-44) — the deterministic economic-calendar blackout gate. Pure function of
// (calendar, now); no sentiment, no interpretation, no fabricated data (INV-4/NFR-14).
public class NewsBlackoutTests
{
    private static readonly DateTimeOffset EventTime = new(2026, 8, 10, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void No_events_never_blocks()
    {
        var gate = new NewsBlackoutGate(new StaticEconomicCalendarProvider());
        Assert.Null(gate.ActiveBlackout(EventTime));
    }

    [Fact]
    public void Inside_window_of_a_high_impact_event_blocks()
    {
        var calendar = new StaticEconomicCalendarProvider(new[]
        {
            new EconomicEvent(EventTime, "FOMC Rate Decision", EventImpact.High),
        });
        var gate = new NewsBlackoutGate(calendar, window: TimeSpan.FromMinutes(30));

        Assert.NotNull(gate.ActiveBlackout(EventTime));                          // exactly at the event
        Assert.NotNull(gate.ActiveBlackout(EventTime.AddMinutes(29)));           // just inside, after
        Assert.NotNull(gate.ActiveBlackout(EventTime.AddMinutes(-29)));          // just inside, before
    }

    [Fact]
    public void Outside_window_of_a_high_impact_event_does_not_block()
    {
        var calendar = new StaticEconomicCalendarProvider(new[]
        {
            new EconomicEvent(EventTime, "FOMC Rate Decision", EventImpact.High),
        });
        var gate = new NewsBlackoutGate(calendar, window: TimeSpan.FromMinutes(30));

        Assert.Null(gate.ActiveBlackout(EventTime.AddMinutes(31)));
        Assert.Null(gate.ActiveBlackout(EventTime.AddMinutes(-31)));
    }

    [Theory]
    [InlineData(EventImpact.Low)]
    [InlineData(EventImpact.Medium)]
    public void Non_high_impact_events_never_block(EventImpact impact)
    {
        var calendar = new StaticEconomicCalendarProvider(new[]
        {
            new EconomicEvent(EventTime, "US CPI Release", impact),
        });
        var gate = new NewsBlackoutGate(calendar, window: TimeSpan.FromMinutes(30));

        Assert.Null(gate.ActiveBlackout(EventTime)); // exactly at the event, still not High
    }

    [Fact]
    public void Active_blackout_returns_the_specific_event()
    {
        var target = new EconomicEvent(EventTime, "FOMC Rate Decision", EventImpact.High);
        var calendar = new StaticEconomicCalendarProvider(new[]
        {
            new EconomicEvent(EventTime.AddDays(-5), "Old High Event", EventImpact.High),
            target,
        });
        var gate = new NewsBlackoutGate(calendar, window: TimeSpan.FromMinutes(30));

        var active = gate.ActiveBlackout(EventTime);
        Assert.Equal(target, active);
    }
}
