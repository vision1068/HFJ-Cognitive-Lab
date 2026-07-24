namespace GoldSignalAnalyzer.Application.Security;

/// <summary>
/// The single, reviewable denylist of secret property/key names (arch §5, A09). One source of
/// truth shared by the Serilog masking enricher, the AppSettings repository (gate B3), the
/// config-export/no-secret tests (gate B3/QA-F8) and the committed-config scan (gate B4).
///
/// Matching is EXACT and case-insensitive (NOT substring) so operational, non-secret values
/// such as Mt5TerminalPath and ServerName are never over-masked (gate QA-F12).
/// </summary>
public static class SecretDenylist
{
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "pwd",
        "token",
        "apikey",
        "apitoken",
        "accesstoken",
        "refreshtoken",
        "secret",
        "investorpassword",
        "account",
        "accountnumber",
        "login",
        "connectionstring",
        "credential",
        "telegrambottoken",
        "emailpassword"
    };

    /// <summary>Exact, case-insensitive membership test.</summary>
    public static bool IsSecret(string? name) => name is not null && Names.Contains(name);
}
