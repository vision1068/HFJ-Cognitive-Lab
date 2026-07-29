using System.Text.Json.Serialization;

namespace GoldSignalAnalyzer.MT5Bridge.Protocol;

/// <summary>
/// Outbound POST body to the bridge (server.py do_POST reads <c>protocolVersion</c>, <c>command</c>,
/// <c>params</c>). The server is CASE-SENSITIVE (server.py:72,76,77), so the emitted JSON keys are
/// pinned via <see cref="JsonPropertyNameAttribute"/> to EXACTLY <c>protocolVersion</c>,
/// <c>command</c>, <c>params</c> (C13) — never the PascalCase System.Text.Json default.
///
/// No property name matches <c>SecretDenylist</c> (the token travels only as the Authorization
/// header value, never in a DTO field).
/// </summary>
public sealed class BridgeRequest
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; init; } = BridgeProtocol.Version;

    [JsonPropertyName("command")]
    public string Command { get; init; } = string.Empty;

    /// <summary>
    /// Command parameters. The DICTIONARY KEYS are the wire keys the Python handlers read
    /// (e.g. <c>symbol</c>, <c>timeframe</c>, <c>count</c>, <c>start</c>, <c>date_from</c>,
    /// <c>date_to</c>) and are supplied verbatim by the client — System.Text.Json emits dictionary
    /// keys as-is, so casing here is controlled at the call site, not by naming policy.
    /// </summary>
    [JsonPropertyName("params")]
    public IReadOnlyDictionary<string, object?> Params { get; init; }
        = new Dictionary<string, object?>();
}
