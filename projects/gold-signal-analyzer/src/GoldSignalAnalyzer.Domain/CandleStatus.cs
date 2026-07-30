namespace GoldSignalAnalyzer.Domain;

/// <summary>
/// FR-11.5 / FR-14: whether a candle is fully closed or still forming.
/// Confirmed signals must use <see cref="Completed"/> candles only; any value
/// derived from a <see cref="Provisional"/> (current, not-yet-closed) candle
/// must be flagged as such and never treated as a confirmed reading.
/// </summary>
public enum CandleStatus
{
    Completed = 0,
    Provisional = 1
}
