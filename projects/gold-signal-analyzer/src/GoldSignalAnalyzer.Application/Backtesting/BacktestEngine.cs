using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Backtesting;

/// <summary>
/// FR-31: a chronological, look-ahead-safe backtest engine. On each bar the
/// strategy receives ONLY a <see cref="LookAheadSafeWindow"/> over history up to
/// and including the current bar (AC-31.1), decides a desired position
/// (Buy=long, Sell=short, Neutral=flat) from the bar close, and the engine fills
/// at that same close with round-trip costs applied (AC-31.2). One position at a
/// time (consistent with FR-25). This simulates on historical candles only — it
/// has no connection to any live order path (INV-1).
/// </summary>
public sealed class BacktestEngine
{
    public BacktestResult Run(
        IReadOnlyList<Candle> candles,
        Func<IReadOnlyList<Candle>, SignalDirection> strategy,
        CostModel? costModel = null,
        int warmup = 0)
    {
        if (candles is null) throw new ArgumentNullException(nameof(candles));
        if (strategy is null) throw new ArgumentNullException(nameof(strategy));
        if (warmup < 0 || warmup >= candles.Count) throw new ArgumentOutOfRangeException(nameof(warmup));
        var cost = costModel ?? new CostModel();

        var trades = new List<BacktestTrade>();
        SignalDirection openDir = SignalDirection.Neutral;
        decimal entryPrice = 0;
        int entryIndex = -1;

        void Close(int exitIndex, decimal exitPrice)
        {
            if (openDir == SignalDirection.Neutral) return;
            decimal gross = openDir == SignalDirection.Buy
                ? exitPrice - entryPrice
                : entryPrice - exitPrice;
            decimal net = gross - cost.RoundTripCost;
            trades.Add(new BacktestTrade(entryIndex, exitIndex, openDir, entryPrice, exitPrice, gross, cost.RoundTripCost, net));
            openDir = SignalDirection.Neutral;
        }

        for (int i = warmup; i < candles.Count; i++)
        {
            var window = new LookAheadSafeWindow(candles, i);
            var desired = strategy(window);
            decimal price = candles[i].Close;

            if (desired != openDir)
            {
                Close(i, price);
                if (desired != SignalDirection.Neutral)
                {
                    openDir = desired;
                    entryPrice = price;
                    entryIndex = i;
                }
            }
        }

        // Close any residual position at the final bar.
        if (openDir != SignalDirection.Neutral)
            Close(candles.Count - 1, candles[^1].Close);

        var metrics = BacktestMetrics.From(trades.Select(t => t.NetPnl).ToList());
        return new BacktestResult(trades, metrics);
    }
}
