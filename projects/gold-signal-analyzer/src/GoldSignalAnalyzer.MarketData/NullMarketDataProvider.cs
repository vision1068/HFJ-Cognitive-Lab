using GoldSignalAnalyzer.Application.Exceptions;
using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.Domain.Entities;
using GoldSignalAnalyzer.Domain.Enums;
using GoldSignalAnalyzer.Domain.ValueObjects;

namespace GoldSignalAnalyzer.MarketData;

/// <summary>
/// Cycle-1 safe default (arch §8, gate R5). Lets DI resolve and the shell run WITHOUT talking to
/// any terminal. Scalar-returning methods THROW <see cref="NotConnectedException"/> rather than
/// return a default Tick/SymbolSpec — a zero-valued instance would be fabricated market data
/// (NFR-5). Collection methods may return empty. There is no MetaTrader5 code in Foundation.
/// </summary>
public sealed class NullMarketDataProvider : IMarketDataProvider
{
    public string Name => "Null (not connected)";

    public bool IsAvailableInProduction => true;

    public Task<ConnectionStatus> ConnectAsync(CancellationToken ct = default)
        => Task.FromResult(ConnectionStatus.NotConnected);

    public Task<IReadOnlyList<SymbolInfo>> GetAvailableSymbolsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<SymbolInfo>>(Array.Empty<SymbolInfo>());

    public Task<SymbolSpec> GetSymbolSpecAsync(string symbol, CancellationToken ct = default)
        => throw new NotConnectedException();

    public Task<IReadOnlyList<Candle>> GetCandlesAsync(string symbol, Timeframe timeframe, int count, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Candle>>(Array.Empty<Candle>());

    public Task<Tick> GetLatestTickAsync(string symbol, CancellationToken ct = default)
        => throw new NotConnectedException();

    public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;
}
