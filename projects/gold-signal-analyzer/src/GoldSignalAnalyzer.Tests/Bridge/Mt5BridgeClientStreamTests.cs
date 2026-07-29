using GoldSignalAnalyzer.Application.Exceptions;
using GoldSignalAnalyzer.Domain.Enums;
using GoldSignalAnalyzer.Domain.ValueObjects;
using GoldSignalAnalyzer.MT5Bridge;
using GoldSignalAnalyzer.MT5Bridge.Protocol;
using Xunit;

namespace GoldSignalAnalyzer.Tests.Bridge;

/// <summary>
/// Stream honesty (C6): the enumerator THROWS mid-enumeration on any non-ok reply (never yields a
/// fabricated value / never silently completes), and <see cref="Candle.IsClosed"/> is derived
/// honestly from the clock — a forming bar is never emitted as closed.
/// </summary>
public sealed class Mt5BridgeClientStreamTests
{
    private static readonly DateTime Now = new(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc);

    private static async Task<Mt5BridgeClient> ConnectedClientAsync(FakeBridgeTransport transport)
    {
        var client = new Mt5BridgeClient(
            new FakeBridgeProcess(), transport, new FakeCredentialStore(), new FixedClock(Now));
        await client.ConnectAsync(CancellationToken.None);
        return client;
    }

    [Fact] // C6
    public async Task StreamTicks_throws_mid_enumeration_on_error()
    {
        var transport = new FakeBridgeTransport { Responder = (_, _) => BridgeResponses.Error(BridgeErrorCodes.HandlerError) };
        await using var client = await ConnectedClientAsync(transport);

        await Assert.ThrowsAsync<NotConnectedException>(async () =>
        {
            await foreach (var _ in client.StreamTicksAsync("XAUUSD", CancellationToken.None))
            {
                // must throw before any tick is yielded
            }
        });
    }

    [Fact] // C6
    public async Task StreamCandles_throws_mid_enumeration_on_error()
    {
        var transport = new FakeBridgeTransport { Responder = (_, _) => BridgeResponses.Error(BridgeErrorCodes.HandlerError) };
        await using var client = await ConnectedClientAsync(transport);

        await Assert.ThrowsAsync<NotConnectedException>(async () =>
        {
            await foreach (var _ in client.StreamCandlesAsync("XAUUSD", Timeframe.H1, CancellationToken.None))
            {
            }
        });
    }

    [Fact] // C6 — a forming bar (interval not yet elapsed) is NEVER IsClosed=true
    public async Task StreamCandles_never_marks_forming_bar_as_closed()
    {
        // Bar opened exactly "now" -> its H1 interval has NOT elapsed -> forming.
        var formingOpen = new DateTimeOffset(Now).ToUnixTimeSeconds();
        var transport = new FakeBridgeTransport
        {
            Responder = (_, _) => BridgeResponses.Ok(new[]
            {
                new { time = formingOpen, open = 1m, high = 2m, low = 0.5m, close = 1.5m, tick_volume = 10.0 },
            }),
        };
        await using var client = await ConnectedClientAsync(transport);

        Candle? first = null;
        await foreach (var c in client.StreamCandlesAsync("XAUUSD", Timeframe.H1, CancellationToken.None))
        {
            first = c;
            break;
        }

        Assert.NotNull(first);
        Assert.False(first!.IsClosed);
    }

    [Fact] // C6 — a bar whose interval has fully elapsed IS closed
    public async Task StreamCandles_marks_elapsed_bar_as_closed()
    {
        // Bar opened 2h ago -> H1 interval elapsed -> closed.
        var closedOpen = new DateTimeOffset(Now.AddHours(-2)).ToUnixTimeSeconds();
        var transport = new FakeBridgeTransport
        {
            Responder = (_, _) => BridgeResponses.Ok(new[]
            {
                new { time = closedOpen, open = 1m, high = 2m, low = 0.5m, close = 1.5m, tick_volume = 10.0 },
            }),
        };
        await using var client = await ConnectedClientAsync(transport);

        Candle? first = null;
        await foreach (var c in client.StreamCandlesAsync("XAUUSD", Timeframe.H1, CancellationToken.None))
        {
            first = c;
            break;
        }

        Assert.NotNull(first);
        Assert.True(first!.IsClosed);
    }

    [Fact] // C6 — the candle poll is pinned to the LAST CLOSED bar (start=1, count=1)
    public async Task StreamCandles_polls_last_closed_bar_position()
    {
        var closedOpen = new DateTimeOffset(Now.AddHours(-2)).ToUnixTimeSeconds();
        IReadOnlyDictionary<string, object?>? captured = null;
        var transport = new FakeBridgeTransport
        {
            Responder = (_, p) =>
            {
                captured = p;
                return BridgeResponses.Ok(new[]
                {
                    new { time = closedOpen, open = 1m, high = 2m, low = 0.5m, close = 1.5m, tick_volume = 10.0 },
                });
            },
        };
        await using var client = await ConnectedClientAsync(transport);

        await foreach (var _ in client.StreamCandlesAsync("XAUUSD", Timeframe.H1, CancellationToken.None))
        {
            break;
        }

        Assert.NotNull(captured);
        Assert.Equal(1, Convert.ToInt32(captured!["start"]));
        Assert.Equal(1, Convert.ToInt32(captured!["count"]));
    }
}
