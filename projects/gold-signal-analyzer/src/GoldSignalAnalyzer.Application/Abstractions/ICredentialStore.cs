namespace GoldSignalAnalyzer.Application.Abstractions;

/// <summary>
/// Access to OS-protected secret storage (Windows Credential Manager / DPAPI).
/// Per NFR-1, MT5 credentials NEVER live in source, appsettings, or logs —
/// only behind this seam. Implementations must never log the secret value.
/// </summary>
public interface ICredentialStore
{
    /// <summary>Returns the secret for a logical key, or null if not present.</summary>
    string? TryGet(string key);

    /// <summary>Persists a secret under a logical key in OS-protected storage.</summary>
    void Store(string key, string secret);

    /// <summary>Removes a stored secret. No-op if absent.</summary>
    void Remove(string key);
}
