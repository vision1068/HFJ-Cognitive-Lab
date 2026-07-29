using System.Text.Json;
using GoldSignalAnalyzer.MT5Bridge.Protocol;
using Xunit;

namespace GoldSignalAnalyzer.Tests.Bridge;

/// <summary>
/// Wire-protocol contract (FR-39 / C13). The Python server reads CASE-SENSITIVE keys
/// <c>protocolVersion</c>, <c>command</c>, <c>params</c> (server.py:72,76,77); a PascalCase default
/// would silently yield <c>protocol_version_mismatch</c>/<c>command_not_allowed</c> at runtime. Also
/// pins the error-code set exhaustiveness that the client's C7 mapping depends on.
/// </summary>
public sealed class BridgeProtocolTests
{
    [Fact] // C13: emitted JSON keys are EXACTLY the lowercase wire keys, never PascalCase
    public void BridgeRequest_serializes_with_exact_lowercase_wire_keys()
    {
        var req = new BridgeRequest
        {
            Command = "copy_rates_range",
            Params = new Dictionary<string, object?> { ["symbol"] = "XAUUSD" },
        };

        var json = JsonSerializer.Serialize(req);
        using var doc = JsonDocument.Parse(json);
        var keys = doc.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray();

        Assert.Equal(new[] { "command", "params", "protocolVersion" }, keys);
        Assert.Equal("1.0", doc.RootElement.GetProperty("protocolVersion").GetString());
        Assert.Equal("copy_rates_range", doc.RootElement.GetProperty("command").GetString());
        Assert.Equal("XAUUSD", doc.RootElement.GetProperty("params").GetProperty("symbol").GetString());

        // Guard against the System.Text.Json PascalCase default leaking onto the wire.
        Assert.DoesNotContain("\"ProtocolVersion\"", json);
        Assert.DoesNotContain("\"Command\"", json);
        Assert.DoesNotContain("\"Params\"", json);
    }

    [Fact] // the exhaustive error-code set the C7 mapping is total over
    public void Error_code_set_is_exactly_the_six_server_codes()
    {
        Assert.Equal(
            new[]
            {
                "command_not_allowed", "handler_error", "malformed_request",
                "not_found", "protocol_version_mismatch", "unauthorized",
            },
            BridgeErrorCodes.All.OrderBy(c => c, StringComparer.Ordinal).ToArray());
    }

    [Fact] // C11: account_info is not among the command constants the client can spell
    public void Command_constants_do_not_include_account_info()
    {
        var commands = new[]
        {
            BridgeCommands.Health, BridgeCommands.SymbolsGet, BridgeCommands.SymbolInfo,
            BridgeCommands.SymbolInfoTick, BridgeCommands.CopyRatesFromPos, BridgeCommands.CopyRatesRange,
        };
        Assert.DoesNotContain("account_info", commands);
    }
}
