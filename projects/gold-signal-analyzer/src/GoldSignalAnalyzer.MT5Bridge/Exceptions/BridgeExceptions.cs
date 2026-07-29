namespace GoldSignalAnalyzer.MT5Bridge.Exceptions;

/// <summary>Base for all bridge-client faults. Never carries the token or any secret in its message.</summary>
public abstract class BridgeException : Exception
{
    protected BridgeException(string message) : base(message) { }
}

/// <summary>
/// The child bridge process failed to start / handshake: no <c>{ready,port}</c> within the timeout,
/// a garbled handshake line, or an early exit before readiness (AC-48.2). The child is killed before
/// this is thrown — no orphan.
/// </summary>
public sealed class BridgeStartException : BridgeException
{
    public BridgeStartException(string message) : base(message) { }
}

/// <summary>Maps the server's <c>unauthorized</c> (401) — the Bearer token was missing/incorrect.</summary>
public sealed class BridgeAuthException : BridgeException
{
    public BridgeAuthException(string message) : base(message) { }
}

/// <summary>
/// Maps the server's <c>protocol_version_mismatch</c> (400), or an unknown/absent error code (a
/// non-<c>ok</c> reply the client cannot classify) — never swallowed as empty.
/// </summary>
public sealed class BridgeProtocolException : BridgeException
{
    public BridgeProtocolException(string message) : base(message) { }
}

/// <summary>Maps the server's <c>command_not_allowed</c> (403) — a command off the read-only allowlist.</summary>
public sealed class BridgeCommandDeniedException : BridgeException
{
    public BridgeCommandDeniedException(string message) : base(message) { }
}

/// <summary>Maps the server's <c>malformed_request</c> (400) and <c>not_found</c> (404).</summary>
public sealed class BridgeRequestException : BridgeException
{
    public BridgeRequestException(string message) : base(message) { }
}

/// <summary>
/// A required field was absent (or the wrong JSON kind) in an <c>ok:true</c> payload. Thrown by the
/// GUARDED field mappings (C8) so a wrong/missing key fails LOUD rather than defaulting to
/// <c>0</c>/<c>0m</c> (which would be fabricated market data, NFR-5).
/// </summary>
public sealed class BridgeMappingException : BridgeException
{
    public BridgeMappingException(string message) : base(message) { }
}
