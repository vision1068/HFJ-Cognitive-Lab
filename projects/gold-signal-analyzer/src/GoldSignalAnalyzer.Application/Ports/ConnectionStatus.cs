namespace GoldSignalAnalyzer.Application.Ports;

/// <summary>Honest connection state for a market-data provider (spec §9). No fabricated data implied.</summary>
public enum ConnectionStatus
{
    NotConnected,
    Connecting,
    Connected,
    Faulted
}
