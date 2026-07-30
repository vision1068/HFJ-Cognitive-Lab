using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Indicators;

/// <summary>Periods for the indicator panel. Sensible defaults; fully configurable.</summary>
public sealed record IndicatorConfig
{
    public int EmaFast { get; init; } = 12;
    public int EmaSlow { get; init; } = 26;
    public int Sma { get; init; } = 20;
    public int MacdFast { get; init; } = 12;
    public int MacdSlow { get; init; } = 26;
    public int MacdSignal { get; init; } = 9;
    public int Rsi { get; init; } = 14;
    public int Atr { get; init; } = 14;
    public int BollingerPeriod { get; init; } = 20;
    public decimal BollingerK { get; init; } = 2m;
    public int Adx { get; init; } = 14;
    public int StochK { get; init; } = 14;
    public int StochD { get; init; } = 3;
    public int VolumeSma { get; init; } = 20;
}

/// <summary>
/// FR-13/FR-14: computes the full indicator panel from a candle series for one
/// timeframe. Each indicator is emitted only when there is enough data —
/// missing indicators are simply absent, never fabricated. The whole snapshot
/// is tagged <see cref="CandleStatus.Provisional"/> if the input's last bar is
/// still forming, so a confirmed signal can require completed inputs (FR-14).
/// </summary>
public sealed class IndicatorEngine
{
    private readonly IndicatorConfig _cfg;
    public IndicatorEngine(IndicatorConfig? config = null) => _cfg = config ?? new IndicatorConfig();

    public IndicatorSnapshot Compute(NormalizedSymbol symbol, TimeFrame tf, IReadOnlyList<Candle> candles)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        if (candles is null) throw new ArgumentNullException(nameof(candles));

        var status = candles.Count > 0 ? candles[^1].Status : CandleStatus.Completed;
        var closes = candles.Select(c => c.Close).ToList();
        var readings = new List<IndicatorReading>();

        void Add(string name, IndicatorCategory cat, decimal value)
            => readings.Add(new IndicatorReading(name, cat, value, tf, status));

        void Try(Action a) { try { a(); } catch (ArgumentException) { /* insufficient data → omit */ } }

        Try(() => Add("EMA_FAST", IndicatorCategory.Trend, Indicators.Ema(closes, _cfg.EmaFast)));
        Try(() => Add("EMA_SLOW", IndicatorCategory.Trend, Indicators.Ema(closes, _cfg.EmaSlow)));
        Try(() => Add("SMA", IndicatorCategory.Trend, Indicators.Sma(closes, _cfg.Sma)));
        Try(() =>
        {
            var m = Indicators.Macd(closes, _cfg.MacdFast, _cfg.MacdSlow, _cfg.MacdSignal);
            Add("MACD_HIST", IndicatorCategory.Trend, m.Histogram);
        });
        Try(() =>
        {
            var a = Indicators.Adx(candles, _cfg.Adx);
            Add("ADX", IndicatorCategory.Trend, a.Adx);
            Add("PLUS_DI", IndicatorCategory.Trend, a.PlusDi);
            Add("MINUS_DI", IndicatorCategory.Trend, a.MinusDi);
        });

        Try(() => Add("RSI", IndicatorCategory.Momentum, Indicators.Rsi(closes, _cfg.Rsi)));
        Try(() =>
        {
            var s = Indicators.Stochastic(candles, _cfg.StochK, _cfg.StochD);
            Add("STOCH_K", IndicatorCategory.Momentum, s.K);
            Add("STOCH_D", IndicatorCategory.Momentum, s.D);
        });

        Try(() => Add("ATR", IndicatorCategory.Volatility, Indicators.Atr(candles, _cfg.Atr)));
        Try(() =>
        {
            var b = Indicators.Bollinger(closes, _cfg.BollingerPeriod, _cfg.BollingerK);
            Add("BB_MIDDLE", IndicatorCategory.Volatility, b.Middle);
            Add("BB_WIDTH", IndicatorCategory.Volatility, b.Width);
        });

        Try(() => Add("OBV", IndicatorCategory.Volume, Indicators.Obv(candles)));
        Try(() => Add("VOL_SMA", IndicatorCategory.Volume, Indicators.VolumeSma(candles, _cfg.VolumeSma)));

        return new IndicatorSnapshot(symbol, tf, status, readings);
    }
}
