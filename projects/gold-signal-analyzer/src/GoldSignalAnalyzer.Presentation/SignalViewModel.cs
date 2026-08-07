using System.Globalization;
using GoldSignalAnalyzer.Application.Analysis;
using GoldSignalAnalyzer.Presentation.Mvvm;

namespace GoldSignalAnalyzer.Presentation;

/// <summary>
/// FR-26: presents a <see cref="SignalAnalysis"/> for display. Every score is
/// labelled "score (0-100)" / "Confidence (0-100)" and is NEVER formatted as a
/// percentage or probability (FR-22). It shows only values the analytical core
/// computed: the explanation lines verbatim, and a risk plan only when the signal is
/// actionable and the plan is tradeable — a rejected plan shows its real reason,
/// never fabricated levels (INV-4/INV-5).
/// </summary>
public sealed class SignalViewModel : ViewModelBase
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private SignalAnalysis? _a;

    /// <summary>Load an analysis to display (or clear with null).</summary>
    public void Load(SignalAnalysis? analysis)
    {
        _a = analysis;
        // Notify every derived property.
        OnPropertyChanged(nameof(HasSignal));
        OnPropertyChanged(nameof(Direction));
        OnPropertyChanged(nameof(IsActionable));
        OnPropertyChanged(nameof(Regime));
        OnPropertyChanged(nameof(DataStatus));
        OnPropertyChanged(nameof(BuyScoreLabel));
        OnPropertyChanged(nameof(SellScoreLabel));
        OnPropertyChanged(nameof(ConfidenceLabel));
        OnPropertyChanged(nameof(PrimaryReason));
        OnPropertyChanged(nameof(ExplanationLines));
        OnPropertyChanged(nameof(HasRiskPlan));
        OnPropertyChanged(nameof(RiskPlanSummary));
    }

    public bool HasSignal => _a is not null;
    public bool IsActionable => _a?.Signal.IsActionable ?? false;
    public string Direction => _a is null ? "—" : _a.Signal.Direction.ToString().ToUpperInvariant();
    public string Regime => _a is null ? "—" : _a.Regime.ToString();
    public string DataStatus => _a is null ? "—" : _a.DataStatus.ToString();

    public string BuyScoreLabel => _a is null
        ? "Buy score (0-100): —"
        : $"Buy score (0-100): {_a.Signal.BuyScore.ToString("0.##", Inv)}";

    public string SellScoreLabel => _a is null
        ? "Sell score (0-100): —"
        : $"Sell score (0-100): {_a.Signal.SellScore.ToString("0.##", Inv)}";

    public string ConfidenceLabel => _a is null
        ? "Confidence (0-100): —"
        : _a.Signal.IsActionable
            ? $"Confidence (0-100, independent of score): {_a.Signal.Confidence.ToString("0.#", Inv)}"
            : "Confidence (0-100): n/a — no actionable signal";

    public string PrimaryReason => _a?.Signal.PrimaryReason ?? "—";

    public IReadOnlyList<string> ExplanationLines => _a?.ExplanationLines ?? Array.Empty<string>();

    public bool HasRiskPlan => _a?.Risk.IsTradeable ?? false;

    /// <summary>
    /// Risk plan for an actionable, tradeable signal; otherwise the real reject
    /// reason. No levels are shown for a Neutral/rejected plan (no fabrication).
    /// </summary>
    public string RiskPlanSummary
    {
        get
        {
            if (_a is null) return "—";
            var r = _a.Risk;
            if (!r.IsTradeable)
                return $"No risk plan: {r.RejectReason}";
            return $"Entry {r.Entry.ToString("0.##", Inv)}  |  "
                 + $"SL {r.StopLoss.ToString("0.##", Inv)}  |  "
                 + $"Target {r.Target.ToString("0.##", Inv)}  |  "
                 + $"R:R {r.RiskReward.ToString("0.##", Inv)}  |  "
                 + $"Size {r.PositionSizeLots.ToString("0.##", Inv)} lots  |  "
                 + $"Risk {r.RiskAmount.ToString("0.##", Inv)}";
        }
    }
}
