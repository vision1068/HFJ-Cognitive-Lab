using GoldSignalAnalyzer.Application.Exceptions;
using GoldSignalAnalyzer.Domain.Enums;
using GoldSignalAnalyzer.MT5Bridge;
using GoldSignalAnalyzer.MT5Bridge.Exceptions;
using GoldSignalAnalyzer.MT5Bridge.Protocol;
using Xunit;

namespace GoldSignalAnalyzer.Tests.Bridge;

/// <summary>
/// Read-path honesty: exhaustive error-code → typed-exception mapping (C7), guarded field mapping
/// (C8), timeframe fail-loud (AC-47.4), and genuine-empty ONLY on ok:true,data:[].
/// </summary>
public sealed class Mt5BridgeClientReadTests
{
    private static readonly DateTime Now = new(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc);

    private static async Task<Mt5BridgeClient> ConnectedClientAsync(
        FakeBridgeTransport transport, FakeBridgeProcess? process = null)
    {
        var client = new Mt5BridgeClient(
            process ?? new FakeBridgeProcess(), transport, new FakeCredentialStore(), new FixedClock(Now));
        await client.ConnectAsync(CancellationToken.None);
        return client;
    }

    // -- C7: exhaustive error-code mapping; NONE maps to empty ------------------------------------

    [Theory]
    [InlineData(BridgeErrorCodes.Unauthorized, typeof(BridgeAuthException))]
    [InlineData(BridgeErrorCodes.CommandNotAllowed, typeof(BridgeCommandDeniedException))]
    [InlineData(BridgeErrorCodes.MalformedRequest, typeof(BridgeRequestException))]
    [InlineData(BridgeErrorCodes.ProtocolVersionMismatch, typeof(BridgeProtocolException))]
    [InlineData(BridgeErrorCodes.NotFound, typeof(BridgeRequestException))]
    [InlineData(BridgeErrorCodes.HandlerError, typeof(NotConnectedException))]
    [InlineData("some_unknown_code", typeof(BridgeProtocolException))]
    public async Task Read_throws_typed_exception_per_error_code(string code, Type expected)
    {
        var transport = new FakeBridgeTransport { Responder = (_, _) => BridgeResponses.Error(code) };
        await using var client = await ConnectedClientAsync(transport);

        var ex = await Record.ExceptionAsync(() => client.GetAvailableSymbolsAsync(CancellationToken.None));

        Assert.NotNull(ex);
        Assert.IsType(expected, ex);
    }

    [Fact] // C7 — empty returned ONLY on a genuine ok:true,data:[]
    public async Task GetAvailableSymbols_returns_empty_only_on_genuine_ok_empty()
    {
        var transport = new FakeBridgeTransport { Responder = (_, _) => BridgeResponses.Ok(Array.Empty<object>()) };
        await using var client = await ConnectedClientAsync(transport);

        var symbols = await client.GetAvailableSymbolsAsync(CancellationToken.None);

        Assert.Empty(symbols);
    }

    [Fact] // C7 — handler_error is NOT swallowed as empty
    public async Task Read_does_not_return_empty_on_handler_error()
    {
        var transport = new FakeBridgeTransport { Responder = (_, _) => BridgeResponses.Error(BridgeErrorCodes.HandlerError) };
        await using var client = await ConnectedClientAsync(transport);

        await Assert.ThrowsAsync<NotConnectedException>(
            () => client.GetAvailableSymbolsAsync(CancellationToken.None));
    }

    // -- AC-47.4: timeframe fail-loud BEFORE any request -----------------------------------------

    [Theory]
    [InlineData(Timeframe.M1)]
    [InlineData(Timeframe.M5)]
    [InlineData(Timeframe.M30)]
    public async Task GetHistoricalCandles_throws_for_unsupported_timeframe_before_any_request(Timeframe tf)
    {
        var transport = new FakeBridgeTransport();
        await using var client = await ConnectedClientAsync(transport);

        await Assert.ThrowsAsync<NotSupportedException>(
            () => client.GetHistoricalCandlesAsync("XAUUSD", tf, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, CancellationToken.None));

        Assert.Empty(transport.Sends); // fail-loud client-side, no request issued
    }

    [Theory]
    [InlineData(Timeframe.M15)]
    [InlineData(Timeframe.H1)]
    [InlineData(Timeframe.H4)]
    [InlineData(Timeframe.D1)]
    public void ToBridgeTimeframe_supports_the_four_slice_timeframes(Timeframe tf)
    {
        Assert.False(string.IsNullOrEmpty(Mt5BridgeClient.ToBridgeTimeframe(tf)));
    }

    // -- C8: guarded field mapping -- a missing key THROWS, never defaults to 0/0m ----------------

    [Fact]
    public async Task SymbolSpecification_mapping_throws_on_missing_key()
    {
        // "digits" deliberately absent.
        var transport = new FakeBridgeTransport
        {
            Responder = (_, _) => BridgeResponses.Ok(new
            {
                trade_contract_size = 100m, trade_tick_size = 0.01m, trade_tick_value = 1m,
                volume_step = 0.01m, volume_min = 0.01m, volume_max = 100m,
            }),
        };
        await using var client = await ConnectedClientAsync(transport);

        await Assert.ThrowsAsync<BridgeMappingException>(
            () => client.GetSymbolSpecificationAsync("XAUUSD", CancellationToken.None));
    }

    [Fact]
    public async Task SymbolSpecification_maps_every_field_explicitly()
    {
        var transport = new FakeBridgeTransport
        {
            Responder = (_, _) => BridgeResponses.Ok(new
            {
                trade_contract_size = 100m, trade_tick_size = 0.01m, trade_tick_value = 1.5m,
                digits = 2, volume_step = 0.01m, volume_min = 0.01m, volume_max = 200m,
            }),
        };
        await using var client = await ConnectedClientAsync(transport);

        var spec = await client.GetSymbolSpecificationAsync("XAUUSD", CancellationToken.None);

        Assert.Equal(100m, spec.ContractSize);
        Assert.Equal(0.01m, spec.TickSize);
        Assert.Equal(1.5m, spec.TickValue);
        Assert.Equal(2, spec.Digits);
        Assert.Equal(0.01m, spec.LotStep);
        Assert.Equal(0.01m, spec.MinLot);
        Assert.Equal(200m, spec.MaxLot);
    }

    [Fact]
    public async Task Candle_mapping_throws_on_missing_key()
    {
        // Rate missing "tick_volume".
        var transport = new FakeBridgeTransport
        {
            Responder = (_, _) => BridgeResponses.Ok(new[]
            {
                new { time = 1_700_000_000L, open = 1m, high = 2m, low = 0.5m, close = 1.5m },
            }),
        };
        await using var client = await ConnectedClientAsync(transport);

        await Assert.ThrowsAsync<BridgeMappingException>(
            () => client.GetHistoricalCandlesAsync("XAUUSD", Timeframe.H1, DateTimeOffset.UnixEpoch, DateTimeOffset.UtcNow, CancellationToken.None));
    }

    [Fact] // C8 — Volume sourced from tick_volume (real_volume commonly 0 on CFD/forex)
    public async Task Candle_volume_maps_from_tick_volume()
    {
        var transport = new FakeBridgeTransport
        {
            Responder = (_, _) => BridgeResponses.Ok(new[]
            {
                new { time = 1_600_000_000L, open = 1m, high = 2m, low = 0.5m, close = 1.5m, tick_volume = 4321.0, real_volume = 0.0 },
            }),
        };
        await using var client = await ConnectedClientAsync(transport);

        var candles = await client.GetHistoricalCandlesAsync(
            "XAUUSD", Timeframe.H1, DateTimeOffset.UnixEpoch, DateTimeOffset.UtcNow, CancellationToken.None);

        var candle = Assert.Single(candles);
        Assert.Equal(4321.0, candle.Volume);
    }
}
