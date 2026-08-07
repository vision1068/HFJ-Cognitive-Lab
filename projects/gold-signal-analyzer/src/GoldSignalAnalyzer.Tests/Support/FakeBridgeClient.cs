using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Bridge;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Tests.Support;

/// <summary>In-memory IMt5BridgeClient for testing the MT5 provider seam.</summary>
public sealed class FakeBridgeClient : IMt5BridgeClient
{
    public BridgeEndpoint Endpoint { get; } = BridgeEndpoint.Loopback(9001);
    public BridgeHealth Health { get; set; } =
        new(true, true, DateTimeOffset.UtcNow);
    public MarketTick? Tick { get; set; }
    public List<BrokerSymbol> Symbols { get; } = new();

    public Task<BridgeHealth> CheckHealthAsync(CancellationToken ct = default) => Task.FromResult(Health);
    public Task<IReadOnlyList<BrokerSymbol>> ListSymbolsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<BrokerSymbol>>(Symbols.ToList());
    public Task<MarketTick?> GetTickAsync(string brokerSymbol, NormalizedSymbol normalized, CancellationToken ct = default)
        => Task.FromResult(Tick);
    public Task<IReadOnlyList<Candle>> GetCandlesAsync(string brokerSymbol, NormalizedSymbol normalized, TimeFrame tf, int count, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Candle>>(Array.Empty<Candle>());
}
