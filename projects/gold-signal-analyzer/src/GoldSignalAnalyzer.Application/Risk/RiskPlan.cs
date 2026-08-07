using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Risk;

/// <summary>
/// FR-25: a proposed (never executed — INV-1) risk plan. If it cannot be built
/// safely (no signal, R:R below the floor, a position already open, or size
/// below the broker minimum lot for the allowed risk), <see cref="IsTradeable"/>
/// is false and <see cref="RejectReason"/> says why — no trade is proposed.
/// </summary>
public sealed record RiskPlan(
    SignalDirection Direction,
    decimal Entry,
    decimal StopLoss,
    decimal Target,
    decimal RiskReward,
    decimal PositionSizeLots,
    decimal RiskAmount,
    bool IsTradeable,
    string? RejectReason)
{
    public static RiskPlan Rejected(SignalDirection dir, string reason) =>
        new(dir, 0, 0, 0, 0, 0, 0, false, reason);
}
