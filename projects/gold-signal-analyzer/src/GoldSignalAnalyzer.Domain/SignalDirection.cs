namespace GoldSignalAnalyzer.Domain;

/// <summary>Direction of a proposed signal. <see cref="Neutral"/> means the
/// product takes no view — the safe default whenever a veto or guard fires.</summary>
public enum SignalDirection
{
    Neutral = 0,
    Buy = 1,
    Sell = 2
}
