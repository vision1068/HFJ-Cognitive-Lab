using GoldSignalAnalyzer.Application.Ports;

namespace GoldSignalAnalyzer.Infrastructure.Security;

/// <summary>
/// Cycle-1 binding for <see cref="ICredentialStore"/> (gate R6, A07). Every member THROWS —
/// it is deliberately NOT a benign null/empty-returning no-op, so any accidental Cycle-1 use of
/// a credential fails loudly rather than silently bypassing an unbuilt secret store.
/// Cycle 2 replaces this with a Windows Credential Manager / DPAPI implementation.
/// </summary>
public sealed class NotImplementedCredentialStore : ICredentialStore
{
    private const string Message =
        "ICredentialStore has no Cycle-1 implementation. Credentials are a Cycle-2 concern " +
        "(Windows Credential Manager / DPAPI). This member must never be a silent no-op (R6/A07).";

    public Task<string?> GetSecretAsync(string key, CancellationToken ct = default)
        => throw new NotImplementedException(Message);

    public Task SetSecretAsync(string key, string secret, CancellationToken ct = default)
        => throw new NotImplementedException(Message);

    public Task DeleteSecretAsync(string key, CancellationToken ct = default)
        => throw new NotImplementedException(Message);
}
