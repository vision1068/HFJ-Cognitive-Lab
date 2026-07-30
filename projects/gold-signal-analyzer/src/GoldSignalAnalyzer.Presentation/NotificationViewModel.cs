using System.Globalization;
using GoldSignalAnalyzer.Application.Analysis;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Presentation;

/// <summary>
/// FR-30 (AC-30.3): a single passive in-app alert about a newly-classified actionable
/// signal. It is a read-only record — it carries the real direction, scores (labelled
/// "score (0-100)", NEVER a percentage/probability — FR-22), confidence and reason
/// from the classification, and exposes NO command/action (INV-1). A notification can
/// never place, simulate, or trigger a trade.
/// </summary>
public sealed class NotificationViewModel
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public NotificationViewModel(DateTimeOffset raisedAtUtc, SignalAnalysis analysis)
    {
        if (analysis is null) throw new ArgumentNullException(nameof(analysis));
        var s = analysis.Signal;

        RaisedAtUtc = raisedAtUtc;
        Direction = s.Direction.ToString().ToUpperInvariant();
        Time = raisedAtUtc.ToString("u", Inv);
        Headline = $"New {Direction} signal";
        BuyScoreLabel = $"Buy score (0-100): {s.BuyScore.ToString("0.##", Inv)}";
        SellScoreLabel = $"Sell score (0-100): {s.SellScore.ToString("0.##", Inv)}";
        ConfidenceLabel = $"Confidence (0-100): {s.Confidence.ToString("0.#", Inv)}";
        Reason = s.PrimaryReason;
        Regime = analysis.Regime.ToString();
    }

    public DateTimeOffset RaisedAtUtc { get; }
    public string Time { get; }
    public string Direction { get; }
    public string Headline { get; }
    public string BuyScoreLabel { get; }
    public string SellScoreLabel { get; }
    public string ConfidenceLabel { get; }
    public string Reason { get; }
    public string Regime { get; }

    /// <summary>One-line summary for a compact list row.</summary>
    public string Summary => $"{Time}  —  {Headline}  ({ConfidenceLabel})";
}
