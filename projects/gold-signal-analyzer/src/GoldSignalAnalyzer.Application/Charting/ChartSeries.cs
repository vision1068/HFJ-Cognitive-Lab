using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Charting;

/// <summary>
/// One OHLC bar reduced to what a chart needs. A projection of an existing
/// <see cref="Candle"/> — it invents nothing (INV-4/INV-5).
/// </summary>
public sealed record ChartBar(
    DateTimeOffset OpenTimeUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close)
{
    /// <summary>Up bar (close ≥ open). Used only to pick a colour (AC-27.3).</summary>
    public bool IsUp => Close >= Open;
}

/// <summary>
/// A named indicator overlay aligned index-for-index to the bar list; a value is
/// <c>null</c> during the indicator's warmup (never a fabricated placeholder).
/// </summary>
public sealed record ChartOverlaySeries(string Name, IReadOnlyList<decimal?> Values);

/// <summary>
/// FR-27 chart payload: the bars plus overlay series and the price extent. It is a
/// pure read-only projection of the analysed candle series — no order surface, no
/// live claim. <see cref="Min"/>/<see cref="Max"/> span the visible price range so a
/// view can scale to a viewport without recomputing anything.
/// </summary>
public sealed record ChartData(
    IReadOnlyList<ChartBar> Bars,
    IReadOnlyList<ChartOverlaySeries> Overlays,
    decimal Min,
    decimal Max)
{
    public bool IsEmpty => Bars.Count == 0;

    public static ChartData Empty { get; } =
        new(Array.Empty<ChartBar>(), Array.Empty<ChartOverlaySeries>(), 0m, 0m);
}
