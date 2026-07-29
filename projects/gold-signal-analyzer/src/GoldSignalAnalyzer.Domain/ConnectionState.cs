namespace GoldSignalAnalyzer.Domain;

/// <summary>
/// Lifecycle state of a market-data connection. Drives NFR-4 behaviour:
/// while not <see cref="Connected"/>, signal generation must stay Neutral.
/// </summary>
public enum ConnectionState
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Reconnecting = 3,
    Faulted = 4
}
