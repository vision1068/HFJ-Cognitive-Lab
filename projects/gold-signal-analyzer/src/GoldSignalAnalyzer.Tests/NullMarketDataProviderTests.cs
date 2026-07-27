using System;
using System.Threading;
using System.Threading.Tasks;
using GoldSignalAnalyzer.Application.Exceptions;
using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.Domain.Enums;
using GoldSignalAnalyzer.MarketData;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE R5: the non-nullable scalar (GetSymbolSpecificationAsync) throws NotConnectedException
/// rather than fabricate a default spec (NFR-5). The nullable account snapshot honestly returns
/// null; collection and streaming members yield nothing; ConnectAsync reports NotConnected.
/// Updated for the byte-exact §9 interface (correction — see CYCLE2-SLICE1-CORRECTION.md).
/// </summary>
public sealed class NullMarketDataProviderTests
{
    private readonly NullMarketDataProvider _sut = new();

    [Fact]
    public async Task R5_GetSymbolSpecificationAsync_throws_not_default()
        => await Assert.ThrowsAsync<NotConnectedException>(
            () => _sut.GetSymbolSpecificationAsync("XAUUSD", CancellationToken.None));

    [Fact]
    public async Task R5_GetAccountSnapshotAsync_returns_null_not_fabricated()
        => Assert.Null(await _sut.GetAccountSnapshotAsync(CancellationToken.None));

    [Fact]
    public async Task R5_provider_name_and_connect_report_not_connected()
    {
        Assert.Equal("Null (not connected)", _sut.ProviderName);
        var result = await _sut.ConnectAsync(CancellationToken.None);
        Assert.Equal(ConnectionStatus.NotConnected, result.Status);
    }

    [Fact]
    public async Task R5_collections_are_empty()
    {
        Assert.Empty(await _sut.GetAvailableSymbolsAsync(CancellationToken.None));
        Assert.Empty(await _sut.GetHistoricalCandlesAsync(
            "XAUUSD", Timeframe.H1, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow, CancellationToken.None));
    }

    [Fact]
    public async Task R5_streams_yield_nothing_never_fabricated()
    {
        var ticks = 0;
        await foreach (var _ in _sut.StreamTicksAsync("XAUUSD", CancellationToken.None))
            ticks++;
        Assert.Equal(0, ticks);

        var candles = 0;
        await foreach (var _ in _sut.StreamCandlesAsync("XAUUSD", Timeframe.H1, CancellationToken.None))
            candles++;
        Assert.Equal(0, candles);
    }
}
