namespace GoldSignalAnalyzer.Domain.Enums;

/// <summary>Direction of an emitted signal (spec §17). Buy/Sell scored independently; Neutral on veto.</summary>
public enum SignalDirection
{
    Buy,
    Sell,
    Neutral
}
