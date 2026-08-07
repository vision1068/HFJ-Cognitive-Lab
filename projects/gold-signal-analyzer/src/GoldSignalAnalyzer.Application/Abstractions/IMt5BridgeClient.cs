using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Application.Bridge;

namespace GoldSignalAnalyzer.Application.Abstractions;

/// <summary>
/// FR-8: the .NET-side seam to the local Python MT5 bridge. The bridge is a
/// localhost-only, token-authenticated HTTP service; this interface hides that
/// transport so MetaTrader5MarketDataProvider is testable without a real
/// terminal or a running bridge process (NFR-10).
/// </summary>
public interface IMt5BridgeClient
{
    /// <summary>Loopback-validated endpoint the client talks to (NFR-2).</summary>
    BridgeEndpoint Endpoint { get; }

    /// <summary>Heartbeat probe (NFR-4). Never throws for an unhealthy bridge —
    /// returns a non-healthy <see cref="BridgeHealth"/> instead.</summary>
    Task<BridgeHealth> CheckHealthAsync(CancellationToken ct = default);

    Task<IReadOnlyList<BrokerSymbol>> ListSymbolsAsync(CancellationToken ct = default);

    Task<MarketTick?> GetTickAsync(
        string brokerSymbol, NormalizedSymbol normalized, CancellationToken ct = default);

    Task<IReadOnlyList<Candle>> GetCandlesAsync(
        string brokerSymbol, NormalizedSymbol normalized, TimeFrame timeFrame, int count, CancellationToken ct = default);
}
