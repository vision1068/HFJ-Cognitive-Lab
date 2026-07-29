using System.Net;
using GoldSignalAnalyzer.Application.Bridge;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Infrastructure.Bridge;
using GoldSignalAnalyzer.Tests.Support;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// FR-8 / NFR-2: token on every request, loopback only, heartbeat degrades gracefully.
public class HttpBridgeClientTests
{
    private static (HttpMt5BridgeClient client, StubHttpMessageHandler handler) Build(Action<StubHttpMessageHandler> setup)
    {
        var ep = BridgeEndpoint.Loopback(9001);
        var handler = new StubHttpMessageHandler();
        setup(handler);
        var http = new HttpClient(handler) { BaseAddress = ep.BaseUri };
        return (new HttpMt5BridgeClient(http, ep, "secret-token-123"), handler);
    }

    [Fact]
    public void Constructor_requires_a_token()
    {
        var ep = BridgeEndpoint.Loopback(9001);
        Assert.Throws<ArgumentException>(() => new HttpMt5BridgeClient(new HttpClient(), ep, ""));
    }

    [Fact]
    public async Task Every_request_carries_the_auth_token_header()
    {
        var (client, handler) = Build(h =>
            h.Map("/symbols", HttpStatusCode.OK, "{\"symbols\":[{\"raw\":\"XAUUSD\",\"description\":\"Gold\"}]}"));

        await client.ListSymbolsAsync();

        var req = Assert.Single(handler.Requests);
        Assert.True(req.Headers.Contains(HttpMt5BridgeClient.TokenHeader));
        Assert.Equal("secret-token-123", req.Headers.GetValues(HttpMt5BridgeClient.TokenHeader).Single());
    }

    [Fact]
    public async Task Health_ok_reports_healthy_and_terminal_connected()
    {
        var (client, _) = Build(h =>
            h.Map("/health", HttpStatusCode.OK, "{\"status\":\"ok\",\"terminalConnected\":true,\"build\":\"MT5 4260\"}"));

        var health = await client.CheckHealthAsync();
        Assert.True(health.IsHealthy);
        Assert.True(health.TerminalConnected);
    }

    [Fact]
    public async Task Health_401_degrades_to_unhealthy_without_throwing()
    {
        var (client, _) = Build(h => h.Map("/health", HttpStatusCode.Unauthorized, null));
        var health = await client.CheckHealthAsync();
        Assert.False(health.IsHealthy);
        Assert.Contains("unauthorized", health.Detail);
    }

    [Fact]
    public async Task Health_unreachable_degrades_to_unhealthy_without_throwing()
    {
        // No route mapped for /health beyond default 404 -> unhealthy, not an exception.
        var (client, _) = Build(_ => { });
        var health = await client.CheckHealthAsync();
        Assert.False(health.IsHealthy);
    }

    [Fact]
    public async Task Unauthorized_data_call_throws()
    {
        var (client, _) = Build(h => h.Map("/symbols", HttpStatusCode.Unauthorized, null));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => client.ListSymbolsAsync());
    }

    [Fact]
    public async Task Tick_is_parsed_into_a_market_tick()
    {
        var (client, _) = Build(h => h.Map("/tick", HttpStatusCode.OK,
            "{\"symbol\":\"XAUUSD\",\"bid\":2400.1,\"ask\":2400.3,\"time\":\"2026-07-30T12:00:00Z\"}"));

        var tick = await client.GetTickAsync("XAUUSD", NormalizedSymbol.Gold);
        Assert.NotNull(tick);
        Assert.Equal(2400.2m, tick!.Mid);
        Assert.Equal(TimeSpan.Zero, tick.TimestampUtc.Offset);
    }

    [Fact]
    public async Task Tick_404_returns_null_not_a_fabricated_price()
    {
        var (client, _) = Build(h => h.Map("/tick", HttpStatusCode.NotFound, null));
        var tick = await client.GetTickAsync("XAUUSD", NormalizedSymbol.Gold);
        Assert.Null(tick);
    }
}
