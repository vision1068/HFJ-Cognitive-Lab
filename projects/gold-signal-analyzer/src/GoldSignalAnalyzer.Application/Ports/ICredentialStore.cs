namespace GoldSignalAnalyzer.Application.Ports;

/// <summary>
/// Deferred credential port (arch ADR-6, gate R6). There is NO Cycle-1 implementation that
/// stores or returns a secret; the Cycle-1 binding (NotImplementedCredentialStore) throws on
/// every member. Cycle 2 implements this over Windows Credential Manager / DPAPI only.
/// </summary>
public interface ICredentialStore
{
    Task<string?> GetSecretAsync(string key, CancellationToken ct = default);
    Task SetSecretAsync(string key, string secret, CancellationToken ct = default);
    Task DeleteSecretAsync(string key, CancellationToken ct = default);
}
