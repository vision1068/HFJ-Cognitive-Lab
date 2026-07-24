using GoldSignalAnalyzer.Application.Exceptions;
using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.Domain.Enums;
using GoldSignalAnalyzer.MarketData;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE R5: every scalar-returning method throws NotConnectedException (never a fabricated default
/// Tick/SymbolSpec, NFR-5). Collection methods return empty; ConnectAsync honestly reports NotConnected.
/// </summary>
public sealed class NullMarketDataProviderTests
{
    private readonly NullMarketDataProvider _sut = new();

    [Fact]
    public async Task R5_GetLatestTickAsync_throws_not_default()
        => await Assert.ThrowsAsync<NotConnectedException>(() => _sut.GetLatestTickAsync("XAUUSD"));

    [Fact]
    public async Task R5_GetSymbolSpecAsync_throws_not_default()
        => await Assert.ThrowsAsync<NotConnectedException>(() => _sut.GetSymbolSpecAsync("XAUUSD"));

    [Fact]
    public async Task R5_collections_are_empty_and_connect_reports_not_connected()
    {
        Assert.True(_sut.IsAvailableInProduction);
        Assert.Equal(ConnectionStatus.NotConnected, await _sut.ConnectAsync());
        Assert.Empty(await _sut.GetAvailableSymbolsAsync());
        Assert.Empty(await _sut.GetCandlesAsync("XAUUSD", Timeframe.H1, 10));
    }
}
