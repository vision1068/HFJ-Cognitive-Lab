namespace GoldSignalAnalyzer.MT5Bridge.Process;

/// <summary>
/// Injectable child-process seam (FR-48). Lets <c>Mt5BridgeClient</c> be unit-tested with a
/// <c>FakeBridgeProcess</c> — no Python interpreter, no spawned process (NFR-12). The concrete
/// implementation is <see cref="PythonBridgeProcess"/>.
///
/// Lifetime contract: <see cref="StartAsync"/> spawns the bridge, writes the token as the FIRST
/// stdin line only, parses the <c>{ready,port}</c> handshake under a timeout, and returns the port.
/// <see cref="Kill"/> is idempotent and kills the whole process tree (graceful-path no-orphan
/// guarantee, C5). Both <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/> kill the child.
/// </summary>
public interface IBridgeProcess : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Spawn the bridge, deliver <paramref name="token"/> via the first stdin line ONLY (never an
    /// arg, never an env var — AC-48.1), parse <c>{"ready":true,"port":N}</c> under a bounded
    /// timeout, and return the integer port. On timeout/garble/early-exit the child is killed and a
    /// <c>BridgeStartException</c> is thrown (AC-48.2). Any <c>host</c> field in the handshake is
    /// ignored (AC-48.3).
    /// </summary>
    Task<int> StartAsync(string token, CancellationToken cancellationToken);

    /// <summary>True once the child has exited (crash/early-exit detection, AC-48.5) or never started.</summary>
    bool HasExited { get; }

    /// <summary>Kill the child process tree. Idempotent — safe to call repeatedly (AC-48.4).</summary>
    void Kill();
}
