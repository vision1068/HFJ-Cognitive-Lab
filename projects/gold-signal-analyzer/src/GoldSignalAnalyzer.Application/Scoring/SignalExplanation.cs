using System.Globalization;
using System.Text;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Scoring;

/// <summary>
/// FR-24: builds a deterministic, data-derived explanation. Every line maps to a
/// real contribution / penalty / veto — no invented facts (INV-5). FR-22: scores
/// are always labelled "score (0–100)", never rendered as an "N% probability".
/// Same inputs ⇒ byte-identical text.
/// </summary>
public static class SignalExplanation
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static IReadOnlyList<string> Lines(SignalClassification c)
    {
        if (c is null) throw new ArgumentNullException(nameof(c));
        var s = c.Score;
        var lines = new List<string>
        {
            $"Regime: {s.Regime} (weights T={s.Weights.Trend} M={s.Weights.Momentum} V={s.Weights.Volatility} Vol={s.Weights.Volume}).",
            $"Buy score (0-100): {c.BuyScore.ToString("0.##", Inv)}  |  Sell score (0-100): {c.SellScore.ToString("0.##", Inv)}.",
        };

        foreach (var e in s.BuyEvidence)
            lines.Add($"  [BUY +{e.WeightedPoints.ToString("0.##", Inv)}] {e.Indicator} ({e.Category}): {e.Note} — raw {e.RawPoints.ToString("0.##", Inv)}, cap {e.Cap.ToString("0.##", Inv)}, capped {e.CappedPoints.ToString("0.##", Inv)}, weight {e.Weight.ToString("0.##", Inv)}.");
        foreach (var e in s.SellEvidence)
            lines.Add($"  [SELL +{e.WeightedPoints.ToString("0.##", Inv)}] {e.Indicator} ({e.Category}): {e.Note} — raw {e.RawPoints.ToString("0.##", Inv)}, cap {e.Cap.ToString("0.##", Inv)}, capped {e.CappedPoints.ToString("0.##", Inv)}, weight {e.Weight.ToString("0.##", Inv)}.");

        foreach (var p in s.Penalties)
            lines.Add($"  [PENALTY -{p.Points.ToString("0.##", Inv)}] {p.Reason}.");
        foreach (var v in s.Vetoes)
            lines.Add($"  [VETO] {v.Reason}.");

        lines.Add($"Decision: {c.Direction} — {c.PrimaryReason}.");
        if (c.IsActionable)
            lines.Add($"Confidence (0-100, independent of score): {c.Confidence.ToString("0.#", Inv)}.");
        return lines;
    }

    public static string Text(SignalClassification c)
    {
        var sb = new StringBuilder();
        foreach (var l in Lines(c)) sb.AppendLine(l);
        return sb.ToString();
    }
}
