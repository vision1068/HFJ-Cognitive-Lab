using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Freshness;

public enum SignalGateStatus { Allowed = 0, Suppressed = 1 }

/// <summary>Outcome of the signal gate. When suppressed, <see cref="Reason"/>
/// is the operator-facing banner text.</summary>
public sealed record SignalGateDecision(SignalGateStatus Status, string? Reason)
{
    public bool IsAllowed => Status == SignalGateStatus.Allowed;
}

/// <summary>
/// FR-12: the veto gate. Signal generation is SUPPRESSED under every stale or
/// invalid condition — not connected, no gold symbol mapped, or data not fresh.
/// The stale banner text is exactly <see cref="StaleReason"/> as specified.
/// Suppression means the product stays Neutral (NFR-4) rather than acting on
/// bad data. Pure function — deterministic and fully unit-testable.
/// </summary>
public sealed class SignalGate
{
    /// <summary>Exact banner text mandated by FR-12.</summary>
    public const string StaleReason = "DATA STALE — SIGNAL GENERATION PAUSED";
    public const string NotConnectedReason = "CONNECTION NOT READY — SIGNAL GENERATION PAUSED";
    public const string NoSymbolReason = "NO GOLD SYMBOL MAPPED — SIGNAL GENERATION PAUSED";

    public SignalGateDecision Evaluate(
        ConnectionState connection,
        FreshnessAssessment freshness,
        bool hasSymbolMapping)
    {
        // Order matters only for the reason shown; ANY failing condition suppresses.
        if (connection != ConnectionState.Connected)
            return new SignalGateDecision(SignalGateStatus.Suppressed, NotConnectedReason);

        if (!hasSymbolMapping)
            return new SignalGateDecision(SignalGateStatus.Suppressed, NoSymbolReason);

        if (freshness.Status != FreshnessStatus.Fresh)
            return new SignalGateDecision(SignalGateStatus.Suppressed, StaleReason);

        return new SignalGateDecision(SignalGateStatus.Allowed, null);
    }
}
