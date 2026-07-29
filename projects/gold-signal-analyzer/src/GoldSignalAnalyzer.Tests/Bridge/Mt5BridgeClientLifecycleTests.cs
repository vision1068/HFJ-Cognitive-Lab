using GoldSignalAnalyzer.Application.Exceptions;
using GoldSignalAnalyzer.MT5Bridge;
using GoldSignalAnalyzer.MT5Bridge.Exceptions;
using Xunit;

namespace GoldSignalAnalyzer.Tests.Bridge;

/// <summary>
/// Child-process lifetime (C5 / ADR-15): every GRACEFUL exit path kills the child (no orphan) and is
/// idempotent; a handshake timeout kills the child and faults; a detected exit makes reads throw
/// (AC-48.5). No real process is spawned.
/// </summary>
public sealed class Mt5BridgeClientLifecycleTests
{
    private static Mt5BridgeClient NewClient(FakeBridgeProcess process, FakeBridgeTransport? transport = null)
        => new(process, transport ?? new FakeBridgeTransport(), new FakeCredentialStore(), new FixedClock(DateTime.UtcNow));

    [Fact] // C5
    public async Task DisconnectAsync_kills_child()
    {
        var process = new FakeBridgeProcess();
        var client = NewClient(process);
        await client.ConnectAsync(CancellationToken.None);

        await client.DisconnectAsync(CancellationToken.None);

        Assert.True(process.Killed);
    }

    [Fact] // C5
    public void Dispose_kills_child()
    {
        var process = new FakeBridgeProcess();
        var client = NewClient(process);

        client.Dispose();

        Assert.True(process.Killed);
    }

    [Fact] // C5
    public async Task DisposeAsync_kills_child()
    {
        var process = new FakeBridgeProcess();
        var client = NewClient(process);

        await client.DisposeAsync();

        Assert.True(process.Killed);
    }

    [Fact] // C5 — idempotent: a second Disconnect/Dispose does not throw
    public async Task Disconnect_then_dispose_is_idempotent()
    {
        var process = new FakeBridgeProcess();
        var client = NewClient(process);
        await client.ConnectAsync(CancellationToken.None);

        await client.DisconnectAsync(CancellationToken.None);
        await client.DisconnectAsync(CancellationToken.None);
        client.Dispose();

        Assert.True(process.Killed);
        Assert.True(process.KillCount >= 1);
    }

    [Fact] // AC-48.2 / C5 — a handshake timeout kills the child and faults
    public async Task Handshake_timeout_kills_child_and_throws_start_exception()
    {
        var process = new FakeBridgeProcess { ThrowOnStart = true };
        await using var client = NewClient(process);

        await Assert.ThrowsAsync<BridgeStartException>(() => client.ConnectAsync(CancellationToken.None));
        Assert.True(process.Killed);
    }

    [Fact] // AC-48.5 — a detected child exit makes reads throw, never fabricate
    public async Task Reads_throw_after_child_exit()
    {
        var process = new FakeBridgeProcess();
        await using var client = NewClient(process);
        await client.ConnectAsync(CancellationToken.None);

        process.HasExited = true; // simulate crash/early-exit

        await Assert.ThrowsAsync<NotConnectedException>(
            () => client.GetAvailableSymbolsAsync(CancellationToken.None));
    }
}
