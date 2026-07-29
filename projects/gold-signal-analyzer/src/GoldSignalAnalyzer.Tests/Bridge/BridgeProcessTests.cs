using GoldSignalAnalyzer.MT5Bridge.Exceptions;
using GoldSignalAnalyzer.MT5Bridge.Process;
using Xunit;

namespace GoldSignalAnalyzer.Tests.Bridge;

/// <summary>
/// PythonBridgeProcess structural security (ADR-15):
///  * AC-48.1 — the spawn descriptor carries the token in NEITHER an argument NOR an env var. The
///    token is delivered only via the child's first stdin line; <see cref="PythonBridgeProcess.BuildStartInfo"/>
///    takes NO token, so a token in args/env is structurally impossible.
///  * AC-48.3 — only the integer port is consumed from the handshake; a malicious <c>host</c> (or any
///    other) field is ignored; a garbled / ready:false / port-less handshake throws.
/// These exercise the <c>internal</c> statics directly (InternalsVisibleTo → Tests). No process spawns.
/// </summary>
public sealed class BridgeProcessTests
{
    // -- AC-48.1: token cannot reach args or env (descriptor is token-free by construction) --------

    [Fact]
    public void BuildStartInfo_runs_the_module_and_carries_no_token_in_args_or_env()
    {
        var options = new BridgeLaunchOptions
        {
            Interpreter = "python",
            PythonRoot = @"C:\some\python\root",
            Module = "bridge.run_bridge",
        };

        var psi = PythonBridgeProcess.BuildStartInfo(options);

        // Runs as a module: `python -m bridge.run_bridge` — exactly two args, no secret among them.
        Assert.Equal(new[] { "-m", "bridge.run_bridge" }, psi.ArgumentList);
        Assert.Equal("python", psi.FileName);

        // Token transport is stdin only: input redirected, no shell, no window.
        Assert.True(psi.RedirectStandardInput);
        Assert.True(psi.RedirectStandardOutput);
        Assert.False(psi.UseShellExecute);
        Assert.True(psi.CreateNoWindow);

        // BuildStartInfo takes NO token parameter, so no argument or environment entry the builder adds
        // could carry one. Assert no env entry was introduced with a token-shaped/secret name.
        foreach (var key in psi.Environment.Keys)
            Assert.DoesNotContain("token", key, StringComparison.OrdinalIgnoreCase);
    }

    // -- AC-48.3: handshake parse ignores host, consumes port only, and fails loud on bad input -----

    [Fact]
    public void ParseHandshakePort_returns_port_and_ignores_a_malicious_host_field()
    {
        // A compromised child tries to redirect the client off loopback via a host field.
        var port = PythonBridgeProcess.ParseHandshakePort(
            "{\"ready\":true,\"port\":49999,\"host\":\"169.254.1.1\"}");

        Assert.Equal(49999, port); // host field is deliberately not read
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("[]")]                              // not an object
    [InlineData("{\"ready\":false,\"port\":5}")]    // ready must be true
    [InlineData("{\"ready\":true}")]                // missing port
    [InlineData("{\"ready\":true,\"port\":0}")]     // non-positive port
    [InlineData("{\"ready\":true,\"port\":\"5\"}")] // port not a number
    [InlineData("")]                                // empty line
    [InlineData("   ")]                             // whitespace
    public void ParseHandshakePort_throws_on_a_garbled_or_incomplete_handshake(string line)
    {
        Assert.Throws<BridgeStartException>(() => PythonBridgeProcess.ParseHandshakePort(line));
    }
}
