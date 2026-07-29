using GoldSignalAnalyzer.MT5Bridge.Protocol;

namespace GoldSignalAnalyzer.MT5Bridge.Transport;

/// <summary>
/// Injectable transport seam (FR-47). Lets <c>Mt5BridgeClient</c> be unit-tested with a
/// <c>FakeBridgeTransport</c> — no Python, no live terminal, no real socket (NFR-12). The concrete
/// loopback implementation is <see cref="LoopbackHttpBridgeTransport"/>.
///
/// <see cref="Bind"/> is called once by the client after the process handshake yields the ephemeral
/// port; it accepts the integer port and the Bearer token ONLY — never a host (loopback is fixed by
/// construction, ADR-14).
/// </summary>
public interface IBridgeTransport : IDisposable
{
    /// <summary>
    /// Fixes the transport to the handshake port and Bearer token. No host is accepted — the
    /// loopback host is a compile-time literal (ADR-14). Idempotent per instance is NOT required;
    /// the client binds exactly once, in ConnectAsync.
    /// </summary>
    void Bind(int port, string token);

    /// <summary>GET /health with the Bearer token. Used ONLY by ConnectAsync (C1).</summary>
    Task<BridgeResponse> GetHealthAsync(CancellationToken cancellationToken);

    /// <summary>POST / with <c>{protocolVersion,command,params}</c> and the Bearer token.</summary>
    Task<BridgeResponse> SendAsync(
        string command,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken);
}
