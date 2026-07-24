namespace GoldSignalAnalyzer.Application.Security;

/// <summary>
/// Thrown when a caller attempts to persist a value under a denylisted secret key (gate B3).
/// The key/value AppSettings table is convention-and-test-guarded to carry no secret.
/// </summary>
public sealed class SecretPersistenceException : InvalidOperationException
{
    public SecretPersistenceException(string key)
        : base($"Rejected attempt to persist a value under denylisted secret key '{key}'. " +
               "Secrets must never live in AppSettings (NFR-1); use ICredentialStore in Cycle 2.")
    {
    }
}
