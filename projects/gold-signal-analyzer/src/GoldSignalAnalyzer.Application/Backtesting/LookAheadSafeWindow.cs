using System.Collections;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Backtesting;

/// <summary>
/// FR-31 (AC-31.1): the ONLY view of history a strategy receives during a
/// backtest bar. It exposes candles [0..currentIndex] and physically throws if
/// asked for any later index — so a strategy cannot peek at a future candle even
/// by accident. Look-ahead safety is a structural property here, not a review note.
/// </summary>
public sealed class LookAheadSafeWindow : IReadOnlyList<Candle>
{
    private readonly IReadOnlyList<Candle> _all;
    private readonly int _len;

    public LookAheadSafeWindow(IReadOnlyList<Candle> all, int currentIndexInclusive)
    {
        _all = all ?? throw new ArgumentNullException(nameof(all));
        if (currentIndexInclusive < 0 || currentIndexInclusive >= all.Count)
            throw new ArgumentOutOfRangeException(nameof(currentIndexInclusive));
        _len = currentIndexInclusive + 1;
    }

    public int Count => _len;

    public Candle this[int index]
    {
        get
        {
            if (index < 0 || index >= _len)
                throw new IndexOutOfRangeException(
                    "Look-ahead prevented: a backtest strategy may not access a future (not-yet-closed) candle.");
            return _all[index];
        }
    }

    public IEnumerator<Candle> GetEnumerator()
    {
        for (int i = 0; i < _len; i++) yield return _all[i];
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
