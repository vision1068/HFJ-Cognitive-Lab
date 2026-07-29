using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.Domain.Enums;
using GoldSignalAnalyzer.MT5Bridge;
using GoldSignalAnalyzer.MT5Bridge.Protocol;
using Xunit;

namespace GoldSignalAnalyzer.Tests.Bridge;

/// <summary>
/// ConnectAsync semantics (C1 / ADR-17), account-snapshot null + no account_info (C11 / ADR-13),
/// and the byte-exact §9 shape (AC-47.1). All with fakes — no Python, no socket, no terminal.
/// </summary>
public sealed class Mt5BridgeClientConnectTests
{
    private static Mt5BridgeClient NewClient(
        FakeBridgeProcess process, FakeBridgeTransport transport, FakeCredentialStore? creds = null)
        => new(process, transport, creds ?? new FakeCredentialStore(), new FixedClock(DateTime.UtcNow));

    [Fact] // C1
    public async Task ConnectAsync_issues_exactly_one_health_and_zero_read_commands()
    {
        var process = new FakeBridgeProcess { HandshakePort = 49222 };
        var transport = new FakeBridgeTransport();
        await using var client = NewClient(process, transport);

        var result = await client.ConnectAsync(CancellationToken.None);

        Assert.Equal(1, transport.HealthCalls);          // exactly one GET /health
        Assert.Empty(transport.Sends);                    // zero POST/read commands
        Assert.Equal(1, process.StartCalls);              // one handshake
        Assert.Equal(49222, transport.BoundPort);         // bound to the handshake port
        Assert.Equal(ConnectionStatus.Connected, result.Status);
        Assert.Contains("no live", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] // C1 — a faulted health is surfaced, not masked as Connected
    public async Task ConnectAsync_faults_when_health_is_not_ok()
    {
        var process = new FakeBridgeProcess();
        var transport = new FakeBridgeTransport
        {
            HealthResponse = BridgeResponses.Error(BridgeErrorCodes.Unauthorized),
        };
        await using var client = NewClient(process, transport);

        await Assert.ThrowsAsync<GoldSignalAnalyzer.MT5Bridge.Exceptions.BridgeAuthException>(
            () => client.ConnectAsync(CancellationToken.None));
    }

    [Fact] // AC-47.5
    public void ProviderName_is_a_constant_and_issues_no_request()
    {
        var transport = new FakeBridgeTransport();
        var client = NewClient(new FakeBridgeProcess(), transport);

        Assert.Equal("MT5 Bridge (read-only)", client.ProviderName);
        Assert.Empty(transport.Sends);
        Assert.Equal(0, transport.HealthCalls);
    }

    [Fact] // C11 / ADR-13
    public async Task GetAccountSnapshotAsync_returns_null_and_issues_no_command()
    {
        var transport = new FakeBridgeTransport();
        await using var client = NewClient(new FakeBridgeProcess(), transport);

        var snapshot = await client.GetAccountSnapshotAsync(CancellationToken.None);

        Assert.Null(snapshot);
        Assert.Empty(transport.Sends);
    }

    [Fact] // C11 — no account_info anywhere across connect + every read
    public async Task Client_never_issues_account_info_command()
    {
        var process = new FakeBridgeProcess();
        var transport = new FakeBridgeTransport
        {
            Responder = (cmd, _) => cmd == BridgeCommands.SymbolInfo
                ? BridgeResponses.Ok(new
                {
                    trade_contract_size = 100m, trade_tick_size = 0.01m, trade_tick_value = 1m,
                    digits = 2, volume_step = 0.01m, volume_min = 0.01m, volume_max = 100m,
                })
                : BridgeResponses.Ok(Array.Empty<object>()),
        };
        await using var client = NewClient(process, transport);

        await client.ConnectAsync(CancellationToken.None);
        await client.GetAvailableSymbolsAsync(CancellationToken.None);
        await client.GetSymbolSpecificationAsync("XAUUSD", CancellationToken.None);
        await client.GetAccountSnapshotAsync(CancellationToken.None);

        Assert.DoesNotContain("account_info", transport.Commands);
    }

    [Fact] // AC-47.1
    public void Mt5BridgeClient_implements_IMarketDataProvider()
    {
        Assert.True(typeof(IMarketDataProvider).IsAssignableFrom(typeof(Mt5BridgeClient)));
    }
}
