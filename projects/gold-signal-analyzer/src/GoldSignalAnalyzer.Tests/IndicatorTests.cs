using GoldSignalAnalyzer.Application.Indicators;
using GoldSignalAnalyzer.Domain;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// Phase 4 — Indicator Engine (FR-13/FR-14/FR-15).
// Every value below is hand-computed with the documented formula, not merely
// asserted to "not throw".
public class IndicatorTests
{
    private static readonly NormalizedSymbol Gold = NormalizedSymbol.Gold;
    private static void Near(decimal expected, decimal actual, decimal tol = 0.0001m)
        => Assert.True(Math.Abs(expected - actual) <= tol, $"expected≈{expected} actual={actual}");

    private static int _seq;
    private static Candle C(decimal h, decimal l, decimal close, decimal vol = 100m, CandleStatus st = CandleStatus.Completed)
        => new(Gold, TimeFrame.M5,
               new DateTimeOffset(2026, 7, 30, 0, 0, 0, TimeSpan.Zero).AddMinutes(5 * _seq++),
               open: l, high: h, low: l, close: close, volume: vol, status: st);

    // ---- SMA / EMA ----

    [Fact]
    public void Sma_last_window() => Assert.Equal(4m, Indicators.Sma(new[] { 1m, 2, 3, 4, 5 }, 3));

    [Fact]
    public void Ema_period3_of_1_to_5_is_4()
    {
        // k=0.5; seed SMA(1,2,3)=2; (4-2)*.5+2=3; (5-3)*.5+3=4
        Near(4m, Indicators.Ema(new[] { 1m, 2, 3, 4, 5 }, 3));
    }

    // ---- RSI (Wilder) ----

    [Fact]
    public void Rsi_period2_of_10_11_10_11_is_75()
    {
        // changes +1,-1,+1; seed avgGain=.5 avgLoss=.5; then gain1 → avgGain=.75 avgLoss=.25; RS=3; RSI=75
        Near(75m, Indicators.Rsi(new[] { 10m, 11, 10, 11 }, 2));
    }

    [Fact]
    public void Rsi_all_gains_is_100() =>
        Assert.Equal(100m, Indicators.Rsi(new[] { 1m, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, 14));

    [Fact]
    public void Rsi_all_losses_is_0() =>
        Assert.Equal(0m, Indicators.Rsi(new[] { 15m, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 }, 14));

    // ---- MACD ----

    [Fact]
    public void Macd_of_linear_series_is_constant_half_with_zero_hist()
    {
        // On [1..5] with 2/3/2: EMA2-EMA3 = 0.5 at every point → signal 0.5 → hist 0
        var m = Indicators.Macd(new[] { 1m, 2, 3, 4, 5 }, fast: 2, slow: 3, signal: 2);
        Near(0.5m, m.Macd);
        Near(0.5m, m.Signal);
        Near(0m, m.Histogram);
    }

    // ---- ATR (Wilder) ----

    [Fact]
    public void Atr_period2_is_3()
    {
        // TR series [2,2,2,4]; seed avg(2,2)=2; (2*1+2)/2=2; (2*1+4)/2=3
        var candles = new[] { C(10, 8, 9), C(11, 9, 10), C(12, 10, 11), C(10, 7, 8) };
        Near(3m, Indicators.Atr(candles, 2));
    }

    // ---- Bollinger ----

    [Fact]
    public void Bollinger_period4_k2()
    {
        // [1,5,1,5] mean=3 popVar=4 std=2 → upper 7 lower -1
        var b = Indicators.Bollinger(new[] { 1m, 5, 1, 5 }, 4, 2m);
        Near(3m, b.Middle);
        Near(7m, b.Upper);
        Near(-1m, b.Lower);
        Near(8m, b.Width);
    }

    // ---- ADX / DI ----

    [Fact]
    public void Adx_pure_uptrend_has_zero_minus_di_and_high_adx()
    {
        var up = new[]
        {
            C(105, 100, 104), C(110, 105, 109), C(115, 110, 114),
            C(120, 115, 119), C(125, 120, 124), C(130, 125, 129),
        };
        var a = Indicators.Adx(up, 2);
        Assert.Equal(0m, a.MinusDi);      // no downward directional movement at all
        Assert.True(a.PlusDi > 0m);
        Assert.True(a.Adx > 99m, $"ADX={a.Adx}");
    }

    [Fact]
    public void Adx_pure_downtrend_has_zero_plus_di()
    {
        var down = new[]
        {
            C(130, 125, 126), C(125, 120, 121), C(120, 115, 116),
            C(115, 110, 111), C(110, 105, 106), C(105, 100, 101),
        };
        var a = Indicators.Adx(down, 2);
        Assert.Equal(0m, a.PlusDi);
        Assert.True(a.MinusDi > 0m);
        Assert.True(a.Adx > 99m, $"ADX={a.Adx}");
    }

    // ---- Stochastic ----

    [Fact]
    public void Stochastic_k_is_50()
    {
        // HH=14 LL=8 close=11 → 100*(11-8)/6 = 50
        var candles = new[] { C(12, 8, 9), C(13, 9, 10), C(14, 10, 11) };
        var s = Indicators.Stochastic(candles, kPeriod: 3, dPeriod: 1);
        Near(50m, s.K);
        Near(50m, s.D);
    }

    // ---- OBV ----

    [Fact]
    public void Obv_accumulates_by_direction()
    {
        // closes 10,11,10,10,12 vols 100,150,200,120,80 → +150 -200 +0 +80 = 30
        var candles = new[]
        {
            C(11, 9, 10, 100), C(12, 10, 11, 150), C(11, 9, 10, 200),
            C(11, 9, 10, 120), C(13, 11, 12, 80),
        };
        Assert.Equal(30m, Indicators.Obv(candles));
    }

    // ---- Engine assembly (FR-14 provisional propagation, FR-15 categories) ----

    private static List<Candle> Trend(int n)
    {
        var list = new List<Candle>();
        decimal p = 1900m;
        for (int i = 0; i < n; i++) { p += 1m; list.Add(C(p + 1, p - 1, p)); }
        return list;
    }

    [Fact]
    public void Engine_computes_panel_with_categories()
    {
        var snap = new IndicatorEngine().Compute(Gold, TimeFrame.H1, Trend(60));
        Assert.NotEmpty(snap.Readings);
        Assert.Equal(IndicatorCategory.Momentum, snap.Get("RSI")!.Category);
        Assert.Equal(IndicatorCategory.Trend, snap.Get("ADX")!.Category);
        Assert.Equal(IndicatorCategory.Volatility, snap.Get("ATR")!.Category);
        Assert.Equal(IndicatorCategory.Volume, snap.Get("OBV")!.Category);
    }

    [Fact]
    public void Engine_tags_snapshot_provisional_when_last_candle_forming()
    {
        var candles = Trend(60);
        candles[^1] = candles[^1].WithStatus(CandleStatus.Provisional);
        var snap = new IndicatorEngine().Compute(Gold, TimeFrame.H1, candles);
        Assert.Equal(CandleStatus.Provisional, snap.Status);
        Assert.All(snap.Readings, r => Assert.Equal(CandleStatus.Provisional, r.Status));
    }

    [Fact]
    public void Engine_omits_indicators_when_insufficient_data_never_fabricates()
    {
        var snap = new IndicatorEngine().Compute(Gold, TimeFrame.H1, Trend(3));
        Assert.Null(snap.Get("ADX"));   // needs 2*14 bars — absent, not invented
        Assert.Null(snap.Get("RSI"));
        Assert.Null(snap.Get("FIB_RETRACE_PCT"));  // needs 50 bars — absent, not invented
        Assert.Null(snap.Get("SWEEP_LOW"));        // needs 21 bars — absent, not invented
    }

    // ---- Fibonacci retracement (Cycle 10, FR-45) ----

    [Fact]
    public void Fibonacci_up_leg_retraces_into_golden_pocket()
    {
        // swing low 100 (first), swing high 110 (after) → up leg; last close 105 → 50% retrace.
        var candles = new[]
        {
            C(101, 100, 100.5m),
            C(105, 102, 104m),
            C(110, 106, 109m),
            C(108, 104, 106m),
            C(106, 104, 105m),
        };
        var fib = Indicators.FibonacciRetracement(candles, lookback: 5);
        Assert.NotNull(fib);
        Assert.Equal(110m, fib!.SwingHigh);
        Assert.Equal(100m, fib.SwingLow);
        Assert.Equal(SignalDirection.Buy, fib.SwingDirection);
        Near(0.5m, fib.RetracementPct);
    }

    [Fact]
    public void Fibonacci_down_leg_mirrors()
    {
        // swing high 110 (first), swing low 100 (after) → down leg; last close 105 → 50% bounce.
        var candles = new[]
        {
            C(110, 106, 109m),
            C(108, 104, 106m),
            C(101, 100, 100.5m),
            C(105, 102, 104m),
            C(106, 104, 105m),
        };
        var fib = Indicators.FibonacciRetracement(candles, lookback: 5);
        Assert.NotNull(fib);
        Assert.Equal(SignalDirection.Sell, fib!.SwingDirection);
        Near(0.5m, fib.RetracementPct);
    }

    [Fact]
    public void Fibonacci_returns_null_on_insufficient_data()
        => Assert.Null(Indicators.FibonacciRetracement(new[] { C(101, 100, 100.5m) }, lookback: 5));

    [Fact]
    public void Fibonacci_returns_null_on_degenerate_flat_window()
    {
        var flat = Enumerable.Range(0, 5).Select(_ => C(100, 100, 100m)).ToArray();
        // A candle requires high >= low; 100/100 is a valid degenerate (zero-range) bar.
        Assert.Null(Indicators.FibonacciRetracement(flat, lookback: 5));
    }

    [Fact]
    public void Fibonacci_returns_null_when_swing_high_and_low_land_on_same_candle()
    {
        // Every candle identical (high 1900.5 / low 1899.5) — the swing high and swing low
        // both resolve to the FIRST candle in the window, so no directional leg exists (D10-6).
        // This is the exact shape of HtfConfirmationTests.Flat() and must not fabricate a lean.
        var flat = Enumerable.Range(0, 10).Select(_ => C(1900.5m, 1899.5m, 1900m)).ToArray();
        Assert.Null(Indicators.FibonacciRetracement(flat, lookback: 10));
    }

    // ---- Liquidity sweep (Cycle 10, FR-46) ----

    [Fact]
    public void LiquiditySweep_detects_swept_low()
    {
        var candles = new[]
        {
            C(105, 102, 103m),
            C(108, 104, 106m),
            C(110, 105, 108m),  // priorHigh = 110
            C(107, 100, 104m),  // priorLow = 100
            C(106, 103, 105m),
            C(105, 99, 101m),   // last: Low 99 < 100, Close 101 > 100 → swept low
        };
        var sweep = Indicators.DetectLiquiditySweep(candles, lookback: 5);
        Assert.True(sweep.SweptLow);
        Assert.False(sweep.SweptHigh);
    }

    [Fact]
    public void LiquiditySweep_detects_swept_high()
    {
        var candles = new[]
        {
            C(105, 102, 103m),
            C(108, 104, 106m),
            C(110, 105, 108m),  // priorHigh = 110
            C(107, 100, 104m),  // priorLow = 100
            C(106, 103, 105m),
            C(112, 105, 108m),  // last: High 112 > 110, Close 108 < 110 → swept high
        };
        var sweep = Indicators.DetectLiquiditySweep(candles, lookback: 5);
        Assert.True(sweep.SweptHigh);
        Assert.False(sweep.SweptLow);
    }

    [Fact]
    public void LiquiditySweep_no_sweep_when_last_candle_inside_prior_range()
    {
        var candles = new[]
        {
            C(105, 102, 103m),
            C(108, 104, 106m),
            C(110, 105, 108m),
            C(107, 100, 104m),
            C(106, 103, 105m),
            C(107, 102, 105m), // fully inside [100,110]
        };
        var sweep = Indicators.DetectLiquiditySweep(candles, lookback: 5);
        Assert.False(sweep.SweptHigh);
        Assert.False(sweep.SweptLow);
    }

    [Fact]
    public void LiquiditySweep_throws_on_insufficient_data_never_fabricates()
        => Assert.Throws<ArgumentException>(() => Indicators.DetectLiquiditySweep(new[] { C(101, 100, 100.5m) }, lookback: 5));

    [Fact]
    public void Engine_categorizes_fib_as_trend_and_sweep_as_momentum()
    {
        var snap = new IndicatorEngine().Compute(Gold, TimeFrame.H1, Trend(60));
        Assert.Equal(IndicatorCategory.Trend, snap.Get("FIB_RETRACE_PCT")!.Category);
        Assert.Equal(IndicatorCategory.Trend, snap.Get("FIB_SWING_DIR")!.Category);
        Assert.Equal(IndicatorCategory.Momentum, snap.Get("SWEEP_LOW")!.Category);
        Assert.Equal(IndicatorCategory.Momentum, snap.Get("SWEEP_HIGH")!.Category);
    }
}
