using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Risk;

/// <summary>FR-25 risk knobs. Defaults: 0.5% risk per trade, min R:R 1.5.</summary>
public sealed record RiskPlanConfig
{
    public decimal RiskPercent { get; init; } = 0.005m;      // 0.5% of balance
    public decimal MinRiskReward { get; init; } = 1.5m;
    public decimal AtrStopMultiple { get; init; } = 1.5m;    // SL distance = k·ATR
    public decimal TargetRewardMultiple { get; init; } = 2.0m; // target distance = r·SL distance
}

/// <summary>
/// FR-25: builds a stop-loss / target / position-size plan from a real
/// <see cref="SymbolSpec"/> and ATR. Enforces the single-open-position rule
/// (AC-25.4), the R:R floor (AC-25.3), and sizes to the risk budget rounding
/// DOWN to the broker lot step so the risk cap is never exceeded. This computes
/// numbers only — it can never place an order (INV-1).
/// </summary>
public sealed class RiskPlanCalculator
{
    private readonly RiskPlanConfig _cfg;
    public RiskPlanCalculator(RiskPlanConfig? cfg = null) => _cfg = cfg ?? new RiskPlanConfig();

    public RiskPlan Build(
        SignalDirection direction,
        decimal entry,
        decimal atr,
        SymbolSpec spec,
        decimal accountBalance,
        bool hasOpenPosition)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        if (direction == SignalDirection.Neutral)
            return RiskPlan.Rejected(direction, "No directional signal — no trade.");
        if (hasOpenPosition)
            return RiskPlan.Rejected(direction, "A position is already open — only one open position permitted.");
        if (entry <= 0) throw new ArgumentOutOfRangeException(nameof(entry));
        if (atr <= 0) throw new ArgumentOutOfRangeException(nameof(atr));
        if (accountBalance <= 0) throw new ArgumentOutOfRangeException(nameof(accountBalance));

        decimal slDist = _cfg.AtrStopMultiple * atr;
        decimal tgtDist = _cfg.TargetRewardMultiple * slDist;

        decimal stop, target;
        if (direction == SignalDirection.Buy) { stop = entry - slDist; target = entry + tgtDist; }
        else { stop = entry + slDist; target = entry - tgtDist; }

        decimal rr = slDist == 0 ? 0 : tgtDist / slDist;
        if (rr < _cfg.MinRiskReward)
            return RiskPlan.Rejected(direction, $"R:R {rr:0.##} below floor {_cfg.MinRiskReward:0.##} — no trade.");

        decimal riskAmount = accountBalance * _cfg.RiskPercent;
        decimal moneyRiskPerLot = slDist * spec.ContractSize;
        decimal rawLots = moneyRiskPerLot <= 0 ? 0 : riskAmount / moneyRiskPerLot;

        // round DOWN to lot step so realised risk never exceeds the budget
        decimal lots = Math.Floor(rawLots / spec.LotStep) * spec.LotStep;
        if (lots < spec.MinLot)
            return RiskPlan.Rejected(direction,
                $"Sizing to the {_cfg.RiskPercent:P2} risk budget yields {rawLots:0.###} lots, below the {spec.MinLot} minimum — no trade.");
        if (lots > spec.MaxLot) lots = spec.MaxLot;

        int d = spec.Digits;
        stop = Math.Round(stop, d);
        target = Math.Round(target, d);
        entry = Math.Round(entry, d);
        decimal realisedRisk = lots * moneyRiskPerLot;

        return new RiskPlan(direction, entry, stop, target, Math.Round(rr, 2), lots, Math.Round(realisedRisk, 2), true, null);
    }
}
