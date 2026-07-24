namespace GoldSignalAnalyzer.Application.Exceptions;

/// <summary>
/// Thrown by a market-data provider when a scalar value (tick/spec) is requested while not
/// connected. Cycle-1 NullMarketDataProvider throws this instead of returning a default
/// (fabricated) value (gate R5, NFR-5).
/// </summary>
public sealed class NotConnectedException : InvalidOperationException
{
    public NotConnectedException(
        string message = "Market data provider is not connected. No live data exists in Cycle 1 (Foundation).")
        : base(message)
    {
    }
}
