using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Indicators;

public sealed record MacdResult(decimal Macd, decimal Signal, decimal Histogram);
public sealed record BollingerResult(decimal Middle, decimal Upper, decimal Lower)
{
    public decimal Width => Upper - Lower;
}
public sealed record AdxResult(decimal Adx, decimal PlusDi, decimal MinusDi);
public sealed record StochasticResult(decimal K, decimal D);

/// <summary>
/// FR-13: pure, local technical-indicator calculators. Every value is computed
/// from candles/closes the app already holds — never from a broker "signal"
/// feed. Money is always <see cref="decimal"/>. Each method throws on
/// insufficient data rather than returning a fabricated placeholder.
/// Verified against fixed datasets with hand-computed expected output.
/// </summary>
public static class Indicators
{
    // ---------- moving averages ----------

    /// <summary>Simple moving average of the last <paramref name="period"/> values.</summary>
    public static decimal Sma(IReadOnlyList<decimal> s, int period)
    {
        Require(s, period, nameof(period));
        decimal sum = 0;
        for (int i = s.Count - period; i < s.Count; i++) sum += s[i];
        return sum / period;
    }

    /// <summary>Full EMA series aligned to the input (null during warmup).</summary>
    public static decimal?[] EmaSeries(IReadOnlyList<decimal> s, int period)
    {
        if (s is null) throw new ArgumentNullException(nameof(s));
        if (period <= 0) throw new ArgumentOutOfRangeException(nameof(period));
        var outp = new decimal?[s.Count];
        if (s.Count < period) return outp;

        decimal k = 2m / (period + 1);
        decimal seed = 0;
        for (int i = 0; i < period; i++) seed += s[i];
        seed /= period;
        outp[period - 1] = seed;

        decimal prev = seed;
        for (int i = period; i < s.Count; i++)
        {
            prev = prev + k * (s[i] - prev);
            outp[i] = prev;
        }
        return outp;
    }

    /// <summary>Last EMA value.</summary>
    public static decimal Ema(IReadOnlyList<decimal> s, int period)
    {
        var series = EmaSeries(s, period);
        for (int i = series.Length - 1; i >= 0; i--)
            if (series[i] is decimal v) return v;
        throw new ArgumentException("Not enough data for EMA.", nameof(s));
    }

    // ---------- MACD ----------

    public static MacdResult Macd(IReadOnlyList<decimal> s, int fast = 12, int slow = 26, int signal = 9)
    {
        if (fast >= slow) throw new ArgumentException("Fast period must be < slow period.");
        var emaFast = EmaSeries(s, fast);
        var emaSlow = EmaSeries(s, slow);

        var macdLine = new List<decimal>();
        for (int i = 0; i < s.Count; i++)
            if (emaFast[i] is decimal f && emaSlow[i] is decimal sl)
                macdLine.Add(f - sl);

        if (macdLine.Count < signal)
            throw new ArgumentException("Not enough data for MACD signal line.", nameof(s));

        decimal signalVal = Ema(macdLine, signal);
        decimal macdVal = macdLine[^1];
        return new MacdResult(macdVal, signalVal, macdVal - signalVal);
    }

    // ---------- RSI (Wilder) ----------

    public static decimal Rsi(IReadOnlyList<decimal> s, int period = 14)
    {
        if (s is null) throw new ArgumentNullException(nameof(s));
        if (period <= 0) throw new ArgumentOutOfRangeException(nameof(period));
        if (s.Count < period + 1) throw new ArgumentException("Not enough data for RSI.", nameof(s));

        decimal avgGain = 0, avgLoss = 0;
        for (int i = 1; i <= period; i++)
        {
            decimal ch = s[i] - s[i - 1];
            if (ch >= 0) avgGain += ch; else avgLoss += -ch;
        }
        avgGain /= period; avgLoss /= period;

        for (int i = period + 1; i < s.Count; i++)
        {
            decimal ch = s[i] - s[i - 1];
            decimal gain = ch > 0 ? ch : 0;
            decimal loss = ch < 0 ? -ch : 0;
            avgGain = (avgGain * (period - 1) + gain) / period;
            avgLoss = (avgLoss * (period - 1) + loss) / period;
        }

        if (avgLoss == 0) return avgGain == 0 ? 50m : 100m;
        decimal rs = avgGain / avgLoss;
        return 100m - 100m / (1m + rs);
    }

    // ---------- ATR (Wilder) ----------

    public static decimal Atr(IReadOnlyList<Candle> candles, int period = 14)
    {
        if (candles is null) throw new ArgumentNullException(nameof(candles));
        if (period <= 0) throw new ArgumentOutOfRangeException(nameof(period));
        if (candles.Count < period) throw new ArgumentException("Not enough data for ATR.", nameof(candles));

        var tr = TrueRanges(candles);
        // seed = SMA of first `period` true ranges
        decimal atr = 0;
        for (int i = 0; i < period; i++) atr += tr[i];
        atr /= period;
        for (int i = period; i < tr.Count; i++)
            atr = (atr * (period - 1) + tr[i]) / period;
        return atr;
    }

    private static List<decimal> TrueRanges(IReadOnlyList<Candle> c)
    {
        var tr = new List<decimal>(c.Count) { c[0].High - c[0].Low };
        for (int i = 1; i < c.Count; i++)
        {
            decimal prevClose = c[i - 1].Close;
            decimal a = c[i].High - c[i].Low;
            decimal b = Math.Abs(c[i].High - prevClose);
            decimal d = Math.Abs(c[i].Low - prevClose);
            tr.Add(Math.Max(a, Math.Max(b, d)));
        }
        return tr;
    }

    // ---------- Bollinger Bands ----------

    public static BollingerResult Bollinger(IReadOnlyList<decimal> s, int period = 20, decimal k = 2m)
    {
        Require(s, period, nameof(period));
        decimal mean = Sma(s, period);
        decimal varSum = 0;
        for (int i = s.Count - period; i < s.Count; i++)
        {
            decimal diff = s[i] - mean;
            varSum += diff * diff;
        }
        decimal variance = varSum / period; // population variance
        decimal std = Sqrt(variance);
        return new BollingerResult(mean, mean + k * std, mean - k * std);
    }

    // ---------- ADX / DI (Wilder) ----------

    public static AdxResult Adx(IReadOnlyList<Candle> c, int period = 14)
    {
        if (c is null) throw new ArgumentNullException(nameof(c));
        if (period <= 0) throw new ArgumentOutOfRangeException(nameof(period));
        if (c.Count < 2 * period) throw new ArgumentException("Not enough data for ADX.", nameof(c));

        int n = c.Count;
        var trs = new decimal[n];
        var plusDm = new decimal[n];
        var minusDm = new decimal[n];
        for (int i = 1; i < n; i++)
        {
            decimal upMove = c[i].High - c[i - 1].High;
            decimal downMove = c[i - 1].Low - c[i].Low;
            plusDm[i] = (upMove > downMove && upMove > 0) ? upMove : 0;
            minusDm[i] = (downMove > upMove && downMove > 0) ? downMove : 0;
            decimal a = c[i].High - c[i].Low;
            decimal b = Math.Abs(c[i].High - c[i - 1].Close);
            decimal d = Math.Abs(c[i].Low - c[i - 1].Close);
            trs[i] = Math.Max(a, Math.Max(b, d));
        }

        // Wilder smoothing seeded by the sum of the first `period` values (indices 1..period)
        decimal trS = 0, plusS = 0, minusS = 0;
        for (int i = 1; i <= period; i++) { trS += trs[i]; plusS += plusDm[i]; minusS += minusDm[i]; }

        var dx = new List<decimal>();
        void AccumulateDx()
        {
            decimal plusDi = trS == 0 ? 0 : 100m * plusS / trS;
            decimal minusDi = trS == 0 ? 0 : 100m * minusS / trS;
            decimal diSum = plusDi + minusDi;
            dx.Add(diSum == 0 ? 0 : 100m * Math.Abs(plusDi - minusDi) / diSum);
        }
        AccumulateDx(); // DX at index = period

        decimal lastPlusDi = trS == 0 ? 0 : 100m * plusS / trS;
        decimal lastMinusDi = trS == 0 ? 0 : 100m * minusS / trS;

        for (int i = period + 1; i < n; i++)
        {
            trS = trS - trS / period + trs[i];
            plusS = plusS - plusS / period + plusDm[i];
            minusS = minusS - minusS / period + minusDm[i];
            AccumulateDx();
            lastPlusDi = trS == 0 ? 0 : 100m * plusS / trS;
            lastMinusDi = trS == 0 ? 0 : 100m * minusS / trS;
        }

        // ADX = Wilder-smoothed DX; seed = SMA of first `period` DX values
        if (dx.Count < period)
            throw new ArgumentException("Not enough data for ADX smoothing.", nameof(c));
        decimal adx = 0;
        for (int i = 0; i < period; i++) adx += dx[i];
        adx /= period;
        for (int i = period; i < dx.Count; i++)
            adx = (adx * (period - 1) + dx[i]) / period;

        return new AdxResult(adx, lastPlusDi, lastMinusDi);
    }

    // ---------- Stochastic oscillator ----------

    public static StochasticResult Stochastic(IReadOnlyList<Candle> c, int kPeriod = 14, int dPeriod = 3)
    {
        if (c is null) throw new ArgumentNullException(nameof(c));
        if (kPeriod <= 0 || dPeriod <= 0) throw new ArgumentOutOfRangeException(nameof(kPeriod));
        if (c.Count < kPeriod + dPeriod - 1) throw new ArgumentException("Not enough data for Stochastic.", nameof(c));

        var ks = new List<decimal>();
        for (int end = kPeriod - 1; end < c.Count; end++)
        {
            decimal hh = decimal.MinValue, ll = decimal.MaxValue;
            for (int i = end - kPeriod + 1; i <= end; i++)
            {
                if (c[i].High > hh) hh = c[i].High;
                if (c[i].Low < ll) ll = c[i].Low;
            }
            decimal range = hh - ll;
            ks.Add(range == 0 ? 50m : 100m * (c[end].Close - ll) / range);
        }
        decimal k = ks[^1];
        decimal dsum = 0;
        for (int i = ks.Count - dPeriod; i < ks.Count; i++) dsum += ks[i];
        decimal d = dsum / dPeriod;
        return new StochasticResult(k, d);
    }

    // ---------- volume ----------

    public static decimal Obv(IReadOnlyList<Candle> c)
    {
        if (c is null) throw new ArgumentNullException(nameof(c));
        if (c.Count == 0) throw new ArgumentException("Not enough data for OBV.", nameof(c));
        decimal obv = 0;
        for (int i = 1; i < c.Count; i++)
        {
            if (c[i].Close > c[i - 1].Close) obv += c[i].Volume;
            else if (c[i].Close < c[i - 1].Close) obv -= c[i].Volume;
        }
        return obv;
    }

    public static decimal VolumeSma(IReadOnlyList<Candle> c, int period)
    {
        if (c is null) throw new ArgumentNullException(nameof(c));
        var vols = c.Select(x => x.Volume).ToList();
        return Sma(vols, period);
    }

    // ---------- helpers ----------

    /// <summary>Newton-refined decimal square root (avoids double round-trip error at scale).</summary>
    public static decimal Sqrt(decimal x)
    {
        if (x < 0) throw new ArgumentOutOfRangeException(nameof(x));
        if (x == 0) return 0;
        decimal guess = (decimal)Math.Sqrt((double)x);
        if (guess == 0) guess = 1m;
        for (int i = 0; i < 8; i++)
            guess = (guess + x / guess) / 2m;
        return guess;
    }

    private static void Require(IReadOnlyList<decimal> s, int period, string paramName)
    {
        if (s is null) throw new ArgumentNullException(nameof(s));
        if (period <= 0) throw new ArgumentOutOfRangeException(paramName);
        if (s.Count < period) throw new ArgumentException($"Need at least {period} values.", nameof(s));
    }
}
