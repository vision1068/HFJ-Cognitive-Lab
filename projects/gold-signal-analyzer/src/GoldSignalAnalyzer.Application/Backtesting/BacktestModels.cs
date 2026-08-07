using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Backtesting;

/// <summary>FR-31 (AC-31.2): per-trade transaction costs, all in price points.</summary>
public sealed record CostModel(decimal Spread = 0m, decimal SlippagePerTrade = 0m, decimal CommissionPerTrade = 0m)
{
    /// <summary>Round-trip cost charged once per completed trade (entry+exit).</summary>
    public decimal RoundTripCost => Spread + SlippagePerTrade + CommissionPerTrade;
}

/// <summary>One completed trade in a backtest. P/L is in price points per unit.</summary>
public sealed record BacktestTrade(
    int EntryIndex,
    int ExitIndex,
    SignalDirection Direction,
    decimal EntryPrice,
    decimal ExitPrice,
    decimal GrossPnl,
    decimal Cost,
    decimal NetPnl);

/// <summary>FR-31 (AC-31.3): the standard performance metric set.</summary>
public sealed record BacktestMetrics(
    decimal NetPnl,
    int TradeCount,
    decimal WinRate,
    decimal ProfitFactor,
    decimal MaxDrawdown,
    decimal Expectancy)
{
    /// <summary>Compute metrics from the ordered net-P/L of each completed trade.</summary>
    public static BacktestMetrics From(IReadOnlyList<decimal> tradeNetPnls)
    {
        if (tradeNetPnls is null) throw new ArgumentNullException(nameof(tradeNetPnls));
        int n = tradeNetPnls.Count;
        if (n == 0) return new BacktestMetrics(0, 0, 0, 0, 0, 0);

        decimal net = 0, grossProfit = 0, grossLoss = 0;
        int wins = 0;
        decimal equity = 0, peak = 0, maxDd = 0;
        foreach (var p in tradeNetPnls)
        {
            net += p;
            if (p > 0) { grossProfit += p; wins++; }
            else if (p < 0) grossLoss += -p;
            equity += p;
            if (equity > peak) peak = equity;
            decimal dd = peak - equity;
            if (dd > maxDd) maxDd = dd;
        }

        decimal winRate = Math.Round((decimal)wins / n, 4);
        decimal profitFactor = grossLoss == 0
            ? (grossProfit > 0 ? 999m : 0m)
            : Math.Round(grossProfit / grossLoss, 4);
        decimal expectancy = Math.Round(net / n, 4);
        return new BacktestMetrics(Math.Round(net, 4), n, winRate, profitFactor, Math.Round(maxDd, 4), expectancy);
    }
}

/// <summary>Full backtest output.</summary>
public sealed record BacktestResult(IReadOnlyList<BacktestTrade> Trades, BacktestMetrics Metrics);
