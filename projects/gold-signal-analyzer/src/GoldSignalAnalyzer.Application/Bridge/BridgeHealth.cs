namespace GoldSignalAnalyzer.Application.Bridge;

/// <summary>
/// Result of a bridge heartbeat probe (NFR-4). Never carries a secret; the
/// optional detail is for operator diagnostics only.
/// </summary>
public sealed record BridgeHealth(
    bool IsHealthy,
    bool TerminalConnected,
    DateTimeOffset CheckedAtUtc,
    string? Detail = null)
{
    public static BridgeHealth Unhealthy(DateTimeOffset atUtc, string detail)
        => new(false, false, atUtc, detail);
}
