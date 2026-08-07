using GoldSignalAnalyzer.Application.Backtesting;
using GoldSignalAnalyzer.Domain;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// Phase 8 — Backtesting (FR-31).
public class BacktestingTests
{
    private static readonly NormalizedSymbol Gold = NormalizedSymbol.Gold;

    private static List<Candle> Series(params decimal[] closes)
    {
        var list = new List<Candle>();
        var t = new DateTimeOffset(2026, 7, 30, 0, 0, 0, TimeSpan.Zero);
        for (int i = 0; i < closes.Length; i++)
        {
            decimal c = closes[i];
            list.Add(new Candle(Gold, TimeFrame.H1, t.AddHours(i), open: c, high: c + 1, low: c - 1, close: c, volume: 100));
        }
        return list;
    }

    // ---- AC-31.1: look-ahead safety is structural ----

    [Fact]
    public void Window_exposes_only_history_up_to_current_bar()
    {
        var candles = Series(10, 20, 30, 40, 50);
        var w = new LookAheadSafeWindow(candles, currentIndexInclusive: 2);
        Assert.Equal(3, w.Count);
        Assert.Equal(30m, w[2].Close);
        Assert.Throws<IndexOutOfRangeException>(() => _ = w[3]); // the future bar
    }

    [Fact]
    public void Strategy_peeking_at_future_candle_throws_inside_engine()
    {
        var candles = Series(10, 20, 30, 40, 50);
        Assert.Throws<IndexOutOfRangeException>(() =>
            new BacktestEngine().Run(candles, hist =>
            {
                var _ = hist[hist.Count]; // illegal future access
                return SignalDirection.Neutral;
            }));
    }

    // ---- AC-31.3: metrics ----

    [Fact]
    public void Metrics_from_known_trade_pnls()
    {
        var m = BacktestMetrics.From(new[] { 10m, -4m, -3m, 8m, -6m });
        Assert.Equal(5m, m.NetPnl);
        Assert.Equal(5, m.TradeCount);
        Assert.Equal(0.4m, m.WinRate);
        Assert.Equal(Math.Round(18m / 13m, 4), m.ProfitFactor);
        Assert.Equal(7m, m.MaxDrawdown);   // equity 10,6,3,11,5 → peak 11 - trough... 10→3 = 7
        Assert.Equal(1m, m.Expectancy);
    }

    // ---- AC-31.2: chronological run with costs ----

    [Fact]
    public void Always_long_on_uptrend_nets_gain_minus_costs()
    {
        var candles = Series(2000, 2001, 2002, 2003, 2004);
        var result = new BacktestEngine().Run(
            candles, _ => SignalDirection.Buy, new CostModel(Spread: 0.5m), warmup: 0);

        Assert.Single(result.Trades);
        Assert.Equal(4m, result.Trades[0].GrossPnl);   // 2004 - 2000
        Assert.Equal(3.5m, result.Metrics.NetPnl);      // minus 0.5 round-trip
        Assert.Equal(1, result.Metrics.TradeCount);
        Assert.Equal(1m, result.Metrics.WinRate);
    }

    // ---- AC-31.4: splits, walk-forward, overfitting ----

    [Fact]
    public void Split_is_chronological_by_ratio()
    {
        var candles = Series(Enumerable.Range(1, 100).Select(i => (decimal)i + 1m).ToArray());
        var split = DataSplitter.Split(candles, 0.6m, 0.2m);
        Assert.Equal(60, split.Train.Count);
        Assert.Equal(20, split.Validation.Count);
        Assert.Equal(20, split.OutOfSample.Count);
        // ordering preserved: train ends before validation begins
        Assert.True(split.Train[^1].OpenTimeUtc < split.Validation[0].OpenTimeUtc);
        Assert.True(split.Validation[^1].OpenTimeUtc < split.OutOfSample[0].OpenTimeUtc);
    }

    [Fact]
    public void WalkForward_rolls_by_test_window()
    {
        var windows = DataSplitter.WalkForward(total: 100, trainSize: 50, testSize: 25);
        Assert.Equal(2, windows.Count);
        Assert.Equal(new WalkForwardWindow(0, 50, 50, 75), windows[0]);
        Assert.Equal(new WalkForwardWindow(25, 75, 75, 100), windows[1]);
    }

    [Fact]
    public void Overfitting_warns_when_validation_collapses()
    {
        var train = new BacktestMetrics(100, 20, 0.6m, 2.0m, 10, 5);
        var badVal = new BacktestMetrics(-10, 10, 0.3m, 0.8m, 20, -1);
        var goodVal = new BacktestMetrics(50, 10, 0.55m, 1.8m, 8, 5);

        Assert.True(DataSplitter.Evaluate(train, badVal).Warn);
        Assert.False(DataSplitter.Evaluate(train, goodVal).Warn);
    }
}
