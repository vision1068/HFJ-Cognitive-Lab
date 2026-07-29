using System.Text.Json;
using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.MT5Bridge.Process;
using GoldSignalAnalyzer.MT5Bridge.Protocol;
using GoldSignalAnalyzer.MT5Bridge.Transport;

namespace GoldSignalAnalyzer.Tests.Bridge;

/// <summary>
/// Records every request the client issues (NFR-12). No Python, no socket, no live terminal. The
/// <see cref="Responder"/> maps a (command, params) to a scripted <see cref="BridgeResponse"/>.
/// </summary>
internal sealed class FakeBridgeTransport : IBridgeTransport
{
    public readonly List<(string Command, IReadOnlyDictionary<string, object?> Params)> Sends = new();
    public int HealthCalls;
    public int? BoundPort;
    public string? BoundToken;

    public BridgeResponse HealthResponse { get; set; } = BridgeResponses.Ok(new { status = "ok", protocolVersion = "1.0" });
    public Func<string, IReadOnlyDictionary<string, object?>, BridgeResponse> Responder { get; set; }
        = (_, _) => BridgeResponses.Ok(Array.Empty<object>());

    public IReadOnlyList<string> Commands => Sends.Select(s => s.Command).ToList();

    public void Bind(int port, string token)
    {
        BoundPort = port;
        BoundToken = token;
    }

    public Task<BridgeResponse> GetHealthAsync(CancellationToken cancellationToken)
    {
        HealthCalls++;
        return Task.FromResult(HealthResponse);
    }

    public Task<BridgeResponse> SendAsync(
        string command, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken)
    {
        Sends.Add((command, parameters));
        return Task.FromResult(Responder(command, parameters));
    }

    public void Dispose() { }
}

/// <summary>
/// Scriptable process seam. Records <see cref="Killed"/>/<see cref="KillCount"/> and the token it
/// received via <see cref="StartAsync"/>. No real process is spawned (NFR-12).
/// </summary>
internal sealed class FakeBridgeProcess : IBridgeProcess
{
    public bool Killed;
    public int KillCount;
    public string? ReceivedToken;
    public int StartCalls;
    public int HandshakePort { get; set; } = 55501;

    /// <summary>When true, <see cref="StartAsync"/> kills the child and throws (handshake timeout, AC-48.2).</summary>
    public bool ThrowOnStart { get; set; }

    /// <summary>Simulated crash/early-exit after a successful start (AC-48.5).</summary>
    public bool HasExited { get; set; }

    public Task<int> StartAsync(string token, CancellationToken cancellationToken)
    {
        StartCalls++;
        ReceivedToken = token;
        if (ThrowOnStart)
        {
            Kill();
            throw new GoldSignalAnalyzer.MT5Bridge.Exceptions.BridgeStartException("simulated handshake timeout");
        }
        return Task.FromResult(HandshakePort);
    }

    public void Kill()
    {
        KillCount++;
        Killed = true;
    }

    public void Dispose() => Kill();

    public ValueTask DisposeAsync()
    {
        Kill();
        return ValueTask.CompletedTask;
    }
}

/// <summary>Fake credential store. Records generated/stored tokens (C10.1 assertions).</summary>
internal sealed class FakeCredentialStore : ICredentialStore
{
    private readonly Dictionary<string, string> _store = new();
    public readonly List<string> SetKeys = new();

    public FakeCredentialStore(params (string Key, string Value)[] seed)
    {
        foreach (var (k, v) in seed) _store[k] = v;
    }

    public Task<string?> GetSecretAsync(string key, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(key, out var v) ? v : null);

    public Task SetSecretAsync(string key, string secret, CancellationToken ct = default)
    {
        _store[key] = secret;
        SetKeys.Add(key);
        return Task.CompletedTask;
    }

    public Task DeleteSecretAsync(string key, CancellationToken ct = default)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }

    public string? Peek(string key) => _store.TryGetValue(key, out var v) ? v : null;
}

/// <summary>Fixed clock for deterministic <c>IsClosed</c> derivation (C6).</summary>
internal sealed class FixedClock : IClock
{
    public FixedClock(DateTime utcNow) => UtcNow = utcNow;
    public DateTime UtcNow { get; set; }
}

/// <summary>Builds <see cref="BridgeResponse"/> instances with real <see cref="JsonElement"/> payloads.</summary>
internal static class BridgeResponses
{
    public static BridgeResponse Ok(object? data)
        => new() { Ok = true, Data = JsonSerializer.SerializeToElement(data) };

    public static BridgeResponse Error(string code, string? detail = null)
        => new() { Ok = false, Error = code, Detail = detail };
}
