using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using GoldSignalAnalyzer.Application.Ports;

namespace GoldSignalAnalyzer.Infrastructure.Security;

/// <summary>
/// Cycle-2 <see cref="ICredentialStore"/> binding (FR-37, RM1/RM3). Secrets are encrypted per-user
/// with Windows DPAPI (<see cref="ProtectedData"/>, <see cref="DataProtectionScope.CurrentUser"/>)
/// and persisted as opaque ciphertext files under the user profile — never in config, DB, process
/// args, or logs. The bridge token lives here and nowhere else (key <c>gsa:mt5-bridge-token</c>).
///
/// The store holds NO broker credential: the tool never collects an MT5 trading/investor password
/// (arch §5.2, AC-40.3). This store exists for the tool's OWN generated bridge token only.
///
/// Round-trips headless on Windows, so Set→Get→Delete yields real command-output evidence (AC-37.4).
///
/// DPAPI (<see cref="ProtectedData"/>) is a Windows-only API and this project targets net8.0, so the
/// class is declared <c>[SupportedOSPlatform("windows")]</c>: the whole product is a WPF
/// (net8.0-windows) desktop tool, so this is a truthful platform declaration — not a suppressed
/// CA1416 warning. Non-Windows callers would be flagged at their call site, which is correct.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsCredentialStore : ICredentialStore
{
    // App-specific additional entropy: binds ciphertext to this app, not a secret itself.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("GoldSignalAnalyzer.v1.dpapi");

    private readonly string _storageDirectory;

    /// <param name="storageDirectory">
    /// Where encrypted blobs are written. Defaults to
    /// <c>%LOCALAPPDATA%\GoldSignalAnalyzer\secrets</c>. Injectable so tests use a temp dir.
    /// </param>
    public WindowsCredentialStore(string? storageDirectory = null)
    {
        _storageDirectory = storageDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GoldSignalAnalyzer", "secrets");
    }

    public Task<string?> GetSecretAsync(string key, CancellationToken ct = default)
    {
        var path = PathFor(key);
        if (!File.Exists(path))
            return Task.FromResult<string?>(null);

        var cipher = File.ReadAllBytes(path);
        var plain = ProtectedData.Unprotect(cipher, Entropy, DataProtectionScope.CurrentUser);
        return Task.FromResult<string?>(Encoding.UTF8.GetString(plain));
    }

    public Task SetSecretAsync(string key, string secret, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(secret);
        Directory.CreateDirectory(_storageDirectory);
        var cipher = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(secret), Entropy, DataProtectionScope.CurrentUser);
        // Write to a temp file then move, so a crash never leaves a half-written blob.
        var path = PathFor(key);
        var tmp = path + ".tmp";
        File.WriteAllBytes(tmp, cipher);
        File.Move(tmp, path, overwrite: true);
        return Task.CompletedTask;
    }

    public Task DeleteSecretAsync(string key, CancellationToken ct = default)
    {
        var path = PathFor(key);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    /// <summary>Map a logical key to a filename via SHA-256 (no raw key on the filesystem).</summary>
    private string PathFor(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var name = Convert.ToHexString(hash).ToLowerInvariant();
        return Path.Combine(_storageDirectory, name + ".dpapi");
    }
}
