namespace GoldSignalAnalyzer.Application.Bridge;

/// <summary>MT5 connection modes required by FR-7.</summary>
public enum Mt5ConnectionMode
{
    /// <summary>Mode A: attach to an already-running terminal session. Never
    /// asks for a trading password.</summary>
    AttachExistingSession = 0,

    /// <summary>Mode B: configured, read-only connection. Never asks for the
    /// Personal-Area password; the read-only credential (if any) is fetched at
    /// connect time via <see cref="Abstractions.ICredentialStore"/>.</summary>
    ConfiguredReadOnly = 1
}

/// <summary>
/// Non-secret connection configuration (FR-7). By deliberate design this type
/// carries NO password / investor-password / PIN / secret field — NFR-1 and
/// the permanent invariant forbid any credential in source or config. Secrets
/// are resolved at runtime through <see cref="Abstractions.ICredentialStore"/>
/// under the logical key <see cref="CredentialKey"/>. The reflection guard test
/// enforces this "no secret member" property so it can never regress.
/// </summary>
public sealed record Mt5ConnectionOptions
{
    public Mt5ConnectionMode Mode { get; init; } = Mt5ConnectionMode.AttachExistingSession;

    /// <summary>Mode B only: account login number (NOT a secret).</summary>
    public long? AccountLogin { get; init; }

    /// <summary>Mode B only: broker server name (NOT a secret).</summary>
    public string? ServerName { get; init; }

    /// <summary>Optional terminal executable path for Mode B launch.</summary>
    public string? TerminalPath { get; init; }

    /// <summary>Logical key used to look up any read-only credential in the OS
    /// credential store — never the credential itself.</summary>
    public string? CredentialKey { get; init; }
}
