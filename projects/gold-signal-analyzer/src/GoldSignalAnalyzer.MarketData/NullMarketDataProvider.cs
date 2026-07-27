using System.Runtime.CompilerServices;
using GoldSignalAnalyzer.Application.Exceptions;
using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.Domain.Entities;
using GoldSignalAnalyzer.Domain.Enums;
using GoldSignalAnalyzer.Domain.ValueObjects;

namespace GoldSignalAnalyzer.MarketData;

/// <summary>
/// Cycle-1 safe default (arch §8, gate R5). Lets DI resolve and the shell run WITHOUT talking to
/// any terminal. It never fabricates market data (NFR-5): the non-nullable scalar
/// <see cref="GetSymbolSpecificationAsync"/> THROWS <see cref="NotConnectedException"/> rather than
/// return a zero-valued spec; the nullable <see cref="GetAccountSnapshotAsync"/> honestly returns
/// <c>null</c>; collection and streaming members yield nothing. There is no MetaTrader5 code here.
/// </summary>
public sealed class NullMarketDataProvider : IMarketDataProvider
{
    public string ProviderName => "Null (not connected)";

    public Task<ProviderConnectionResult> ConnectAsync(CancellationToken cancellationToken)
        => Task.FromResult(new ProviderConnectionResult(ConnectionStatus.NotConnected));

    public Task DisconnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<AccountSnapshot?> GetAccountSnapshotAsync(CancellationToken cancellationToken)
        => Task.FromResult<AccountSnapshot?>(null);

    public Task<IReadOnlyList<MarketSymbol>> GetAvailableSymbolsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<MarketSymbol>>(Array.Empty<MarketSymbol>());

    public Task<SymbolSpecification> GetSymbolSpecificationAsync(string symbol, CancellationToken cancellationToken)
        => throw new NotConnectedException();

    public Task<IReadOnlyList<Candle>> GetHistoricalCandlesAsync(
        string symbol, Timeframe timeframe, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Candle>>(Array.Empty<Candle>());

    public async IAsyncEnumerable<MarketTick> StreamTicksAsync(
        string symbol, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        yield break; // not connected: an honest empty stream, never a fabricated tick (NFR-5)
    }

    public async IAsyncEnumerable<Candle> StreamCandlesAsync(
        string symbol, Timeframe timeframe, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        yield break; // not connected: an honest empty stream, never a fabricated candle (NFR-5)
    }
}
