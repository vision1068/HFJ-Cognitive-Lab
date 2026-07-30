using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Backtesting;

/// <summary>Chronological train / validation / out-of-sample partition.</summary>
public sealed record DataSplit(
    IReadOnlyList<Candle> Train,
    IReadOnlyList<Candle> Validation,
    IReadOnlyList<Candle> OutOfSample);

/// <summary>One walk-forward window: train on [TrainStart,TrainEnd), test on [TestStart,TestEnd).</summary>
public sealed record WalkForwardWindow(int TrainStart, int TrainEnd, int TestStart, int TestEnd);

/// <summary>Result of the overfitting heuristic (AC-31.4).</summary>
public sealed record OverfittingCheck(bool Warn, string Message);

/// <summary>
/// FR-31 (AC-31.4): chronological data splitting, walk-forward window generation,
/// and a simple overfitting heuristic. Splits are strictly time-ordered — no
/// shuffling — so the validation/out-of-sample sets are always AFTER the training
/// data (never leaking the future backwards).
/// </summary>
public static class DataSplitter
{
    public static DataSplit Split(IReadOnlyList<Candle> candles, decimal trainRatio = 0.6m, decimal validationRatio = 0.2m)
    {
        if (candles is null) throw new ArgumentNullException(nameof(candles));
        if (trainRatio <= 0 || validationRatio < 0 || trainRatio + validationRatio >= 1m)
            throw new ArgumentException("Ratios must be positive and leave room for an out-of-sample set.");
        int n = candles.Count;
        int trainEnd = (int)(n * trainRatio);
        int valEnd = trainEnd + (int)(n * validationRatio);
        return new DataSplit(
            candles.Take(trainEnd).ToList(),
            candles.Skip(trainEnd).Take(valEnd - trainEnd).ToList(),
            candles.Skip(valEnd).ToList());
    }

    public static IReadOnlyList<WalkForwardWindow> WalkForward(int total, int trainSize, int testSize)
    {
        if (trainSize <= 0 || testSize <= 0) throw new ArgumentOutOfRangeException(nameof(trainSize));
        var windows = new List<WalkForwardWindow>();
        int start = 0;
        while (start + trainSize + testSize <= total)
        {
            int trainEnd = start + trainSize;
            int testEnd = trainEnd + testSize;
            windows.Add(new WalkForwardWindow(start, trainEnd, trainEnd, testEnd));
            start += testSize; // roll forward by the test window
        }
        return windows;
    }

    /// <summary>Warn when out-of-sample profit factor collapses relative to training —
    /// the classic overfitting fingerprint.</summary>
    public static OverfittingCheck Evaluate(BacktestMetrics train, BacktestMetrics validation, decimal degradeThreshold = 0.5m)
    {
        if (train.ProfitFactor <= 0)
            return new OverfittingCheck(false, "Training produced no edge to over-fit.");
        decimal ratio = validation.ProfitFactor / train.ProfitFactor;
        bool warn = ratio < degradeThreshold;
        string msg = warn
            ? $"OVERFITTING WARNING: validation profit factor {validation.ProfitFactor} is {ratio:P0} of training {train.ProfitFactor} (< {degradeThreshold:P0})."
            : $"Validation held up: profit factor {validation.ProfitFactor} vs training {train.ProfitFactor}.";
        return new OverfittingCheck(warn, msg);
    }
}
