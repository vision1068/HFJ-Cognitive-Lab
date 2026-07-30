using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.MarketData;

/// <summary>
/// FR-14: a set of aligned candle series keyed by timeframe. Provides a
/// "completed candles only" view so a confirmed signal is never computed from a
/// still-forming (Provisional) bar, plus access to the current provisional bar
/// when a caller explicitly wants a provisional reading.
/// </summary>
public sealed class MultiTimeFrameSet
{
    private readonly IReadOnlyDictionary<TimeFrame, IReadOnlyList<Candle>> _byTimeFrame;

    public MultiTimeFrameSet(IReadOnlyDictionary<TimeFrame, IReadOnlyList<Candle>> byTimeFrame)
        => _byTimeFrame = byTimeFrame ?? throw new ArgumentNullException(nameof(byTimeFrame));

    public IReadOnlyCollection<TimeFrame> TimeFrames => (IReadOnlyCollection<TimeFrame>)_byTimeFrame.Keys;

    public bool Has(TimeFrame tf) => _byTimeFrame.ContainsKey(tf);

    /// <summary>All candles for a timeframe (completed + any trailing provisional).</summary>
    public IReadOnlyList<Candle> All(TimeFrame tf)
        => _byTimeFrame.TryGetValue(tf, out var list) ? list : Array.Empty<Candle>();

    /// <summary>FR-14: completed candles only — the input for confirmed signals.</summary>
    public IReadOnlyList<Candle> Completed(TimeFrame tf)
        => All(tf).Where(c => c.Status == CandleStatus.Completed).ToList();

    /// <summary>The current still-forming bar for a timeframe, if any.</summary>
    public Candle? Provisional(TimeFrame tf)
        => All(tf).LastOrDefault(c => c.Status == CandleStatus.Provisional);
}
