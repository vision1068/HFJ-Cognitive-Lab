namespace GoldSignalAnalyzer.MT5Bridge.Process;

/// <summary>
/// Launch parameters for <see cref="PythonBridgeProcess"/> — no hardcoded interpreter/path in the
/// spawner. The bridge is run as a MODULE (<c>python -m bridge.run_bridge</c>) with the working
/// directory set to the python package root, because <c>run_bridge.py</c> does
/// <c>from bridge import ...</c> and running the file directly would not put the package root on
/// <c>sys.path</c>.
///
/// The token is NOT here: it is delivered only via the child's first stdin line (AC-48.1).
/// </summary>
public sealed class BridgeLaunchOptions
{
    /// <summary>Interpreter executable — e.g. <c>py</c> or <c>python</c>. Discovered by the caller.</summary>
    public string Interpreter { get; init; } = "python";

    /// <summary>The python package root (the directory that CONTAINS the <c>bridge</c> package).</summary>
    public string PythonRoot { get; init; } = string.Empty;

    /// <summary>The <c>-m</c> module target. Fixed to the shipped entrypoint.</summary>
    public string Module { get; init; } = "bridge.run_bridge";

    /// <summary>Bound wait for the <c>{ready,port}</c> handshake before the child is killed (AC-48.2).</summary>
    public TimeSpan HandshakeTimeout { get; init; } = TimeSpan.FromSeconds(15);
}
