using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using GoldSignalAnalyzer.Application.Abstractions;

namespace GoldSignalAnalyzer.Infrastructure.Security;

/// <summary>
/// NFR-1: a Windows DPAPI-backed credential store. Secrets are encrypted at
/// rest under the current Windows user (DPAPI CurrentUser scope) and written
/// to a per-user file whose name is a hash of the logical key — the plaintext
/// secret is never written to disk, source, appsettings, or logs. This is the
/// only sanctioned home for any MT5 read-only credential.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiCredentialStore : ICredentialStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("GoldSignalAnalyzer.v1");
    private readonly string _dir;

    public DpapiCredentialStore(string? baseDirectory = null)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("DpapiCredentialStore is Windows-only (DPAPI).");
        _dir = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GoldSignalAnalyzer", "creds");
        Directory.CreateDirectory(_dir);
    }

    private string PathFor(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key required.", nameof(key));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        return Path.Combine(_dir, hash + ".bin");
    }

    public string? TryGet(string key)
    {
        var path = PathFor(key);
        if (!File.Exists(path)) return null;
        var protectedBytes = File.ReadAllBytes(path);
        var plain = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plain);
    }

    public void Store(string key, string secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        var protectedBytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(secret), Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(PathFor(key), protectedBytes);
    }

    public void Remove(string key)
    {
        var path = PathFor(key);
        if (File.Exists(path)) File.Delete(path);
    }
}
