namespace GoldSignalAnalyzer.MT5Bridge.Protocol;

/// <summary>
/// Wire-protocol constants shared with the Python bridge (server.py / commands.py). The version
/// string MUST equal <c>commands.PROTOCOL_VERSION</c> ("1.0"); a mismatch is rejected server-side
/// with <see cref="BridgeErrorCodes.ProtocolVersionMismatch"/>.
/// </summary>
public static class BridgeProtocol
{
    public const string Version = "1.0";
}

/// <summary>
/// The EXHAUSTIVE set of non-<c>ok</c> error codes the Python bridge can return (server.py +
/// commands.dispatch). Kept as constants so the .NET error-code → typed-exception mapping (C7) is
/// total and a new server code cannot silently fall through to an "honest empty" (which would be
/// fabrication-adjacent). Every code here maps to a distinct typed exception in
/// <c>Mt5BridgeClient.MapError</c>.
/// </summary>
public static class BridgeErrorCodes
{
    /// <summary>401 — missing/incorrect Bearer token.</summary>
    public const string Unauthorized = "unauthorized";

    /// <summary>403 — command not on the read-only allowlist.</summary>
    public const string CommandNotAllowed = "command_not_allowed";

    /// <summary>400 — request body was not valid JSON / not an object.</summary>
    public const string MalformedRequest = "malformed_request";

    /// <summary>400 — request protocolVersion did not equal the server's.</summary>
    public const string ProtocolVersionMismatch = "protocol_version_mismatch";

    /// <summary>404 — unknown route (e.g. a GET path other than /health).</summary>
    public const string NotFound = "not_found";

    /// <summary>
    /// 400 — the handler raised (e.g. the gateway's <c>mt5</c> alias is <c>None</c> because no live
    /// terminal is attached this slice). This is the "bridge up, terminal not attached" signal.
    /// </summary>
    public const string HandlerError = "handler_error";

    /// <summary>Every code above, for exhaustiveness assertions in tests.</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Unauthorized,
        CommandNotAllowed,
        MalformedRequest,
        ProtocolVersionMismatch,
        NotFound,
        HandlerError,
    };
}

/// <summary>
/// The read-only command names on the bridge allowlist (commands.ALLOWED_COMMANDS). Constants so the
/// client never spells a command as a bare string literal at a call site. NOTE: <c>account_info</c>
/// is deliberately absent — ADR-13/C11: the client issues no <c>account_info</c> command this slice.
/// </summary>
public static class BridgeCommands
{
    public const string Health = "health";
    public const string SymbolsGet = "symbols_get";
    public const string SymbolInfo = "symbol_info";
    public const string SymbolInfoTick = "symbol_info_tick";
    public const string CopyRatesFromPos = "copy_rates_from_pos";
    public const string CopyRatesRange = "copy_rates_range";
}
