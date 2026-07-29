using System.Text.Json;
using GoldSignalAnalyzer.MT5Bridge.Exceptions;
using SysDiag = System.Diagnostics;

namespace GoldSignalAnalyzer.MT5Bridge.Process;

/// <summary>
/// Spawns and owns the Python bridge child process (ADR-15).
///
/// SECURITY POSTURE:
///  * The token is written as the FIRST stdin line ONLY — never a command-line argument (visible in
///    Task Manager / <c>wmic process</c>) and never an environment variable (a child env block is
///    same-user-readable and captured in WER crash dumps). See <see cref="BuildStartInfo"/>: it takes
///    NO token, so a token in args/env is structurally impossible (AC-48.1).
///  * Only the integer <c>port</c> is consumed from the <c>{ready,port}</c> handshake; any <c>host</c>
///    field is ignored (AC-48.3) — parsing is <see cref="ParseHandshakePort"/>.
///
/// NO-ORPHAN GUARANTEE (C5, GRACEFUL PATHS ONLY): <see cref="Kill"/> runs
/// <c>Process.Kill(entireProcessTree:true)</c> idempotently on spawn-fail, handshake-timeout,
/// crash-detected, DisconnectAsync, and Dispose/DisposeAsync. A HARD-KILL of the .NET HOST (SIGKILL /
/// TerminateProcess of GoldSignalAnalyzer.exe) can still orphan the child — an absolute guarantee
/// would need a Windows Job Object (KILL_ON_JOB_CLOSE), which is Windows-only and would force
/// <c>[SupportedOSPlatform("windows")]</c>, contradicting ADR-16's portable-BCL commitment. Hard-kill
/// hardening is therefore explicitly OUT OF SCOPE this slice (honest documented residual). This type
/// adds NO Windows-only API and carries NO platform attribute.
/// </summary>
public sealed class PythonBridgeProcess : IBridgeProcess
{
    private readonly BridgeLaunchOptions _options;
    private readonly object _gate = new();
    private SysDiag.Process? _process;
    private bool _killed;

    public PythonBridgeProcess(BridgeLaunchOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Build the spawn descriptor. Takes NO token: the token can therefore appear in NEITHER an
    /// argument NOR an environment variable — it is delivered only via stdin in <see cref="StartAsync"/>.
    /// Exposed <c>internal</c> so a test can assert the args/env carry no secret (AC-48.1).
    /// </summary>
    internal static SysDiag.ProcessStartInfo BuildStartInfo(BridgeLaunchOptions options)
    {
        var psi = new SysDiag.ProcessStartInfo
        {
            FileName = options.Interpreter,
            WorkingDirectory = options.PythonRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
        };
        psi.ArgumentList.Add("-m");
        psi.ArgumentList.Add(options.Module);
        return psi;
    }

    /// <summary>
    /// Parse <c>{"ready":true,"port":N}</c>. Returns the integer port and IGNORES any other field
    /// (notably a malicious <c>host</c>, AC-48.3). Throws <see cref="BridgeStartException"/> on a
    /// garbled line, <c>ready!=true</c>, or a missing/invalid port. Exposed <c>internal</c> for tests.
    /// </summary>
    internal static int ParseHandshakePort(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            throw new BridgeStartException("Bridge produced an empty handshake line.");

        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(line);
            root = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new BridgeStartException($"Bridge handshake was not valid JSON: {ex.Message}");
        }

        if (root.ValueKind != JsonValueKind.Object)
            throw new BridgeStartException("Bridge handshake was not a JSON object.");

        if (!root.TryGetProperty("ready", out var ready)
            || ready.ValueKind != JsonValueKind.True)
            throw new BridgeStartException("Bridge handshake did not report ready:true.");

        if (!root.TryGetProperty("port", out var portEl)
            || portEl.ValueKind != JsonValueKind.Number
            || !portEl.TryGetInt32(out var port)
            || port <= 0)
            throw new BridgeStartException("Bridge handshake did not carry a valid integer port.");

        return port; // any "host" (or other) field is deliberately not read (AC-48.3).
    }

    public async Task<int> StartAsync(string token, CancellationToken cancellationToken)
    {
        if (token is null) throw new ArgumentNullException(nameof(token));

        var psi = BuildStartInfo(_options);
        var process = new SysDiag.Process { StartInfo = psi, EnableRaisingEvents = true };

        lock (_gate)
        {
            if (_process is not null)
                throw new InvalidOperationException("Bridge process already started.");
            _process = process;
        }

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            Kill();
            throw new BridgeStartException($"Failed to spawn the bridge interpreter '{_options.Interpreter}': {ex.Message}");
        }

        // Token is the FIRST stdin line and the ONLY token transport (AC-48.1).
        await process.StandardInput.WriteLineAsync(token).ConfigureAwait(false);
        await process.StandardInput.FlushAsync().ConfigureAwait(false);
        process.StandardInput.Close();

        // Read the handshake line under a bounded timeout; timeout/early-exit => kill + throw.
        var readTask = process.StandardOutput.ReadLineAsync();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var delayTask = Task.Delay(_options.HandshakeTimeout, timeoutCts.Token);

        var completed = await Task.WhenAny(readTask, delayTask).ConfigureAwait(false);
        if (completed != readTask)
        {
            Kill();
            throw new BridgeStartException($"Bridge did not handshake within {_options.HandshakeTimeout}.");
        }

        timeoutCts.Cancel(); // stop the delay timer
        string? line;
        try
        {
            line = await readTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Kill();
            throw new BridgeStartException($"Failed to read the bridge handshake: {ex.Message}");
        }

        try
        {
            return ParseHandshakePort(line);
        }
        catch (BridgeStartException)
        {
            Kill();
            throw;
        }
    }

    public bool HasExited
    {
        get
        {
            lock (_gate)
            {
                if (_process is null) return true; // never started == not live
                try { return _process.HasExited; }
                catch { return true; }
            }
        }
    }

    public void Kill()
    {
        lock (_gate)
        {
            if (_killed) return;
            _killed = true;
            try
            {
                if (_process is not null && !_process.HasExited)
                    _process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best-effort: the child may already have exited. No orphan on any graceful path.
            }
        }
    }

    public void Dispose()
    {
        Kill();
        lock (_gate)
        {
            _process?.Dispose();
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
