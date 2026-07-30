using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Scoring;

/// <summary>
/// FR-18: one piece of directional evidence and exactly how it was scored —
/// raw points, the category cap it was subject to, the points after capping,
/// the regime weight applied, and the resulting weighted points. Every field
/// is surfaced in the explanation so nothing is a black box (INV-5).
/// </summary>
public sealed record ScoreContribution(
    string Indicator,
    IndicatorCategory Category,
    SignalDirection Direction,
    decimal RawPoints,
    decimal Cap,
    decimal CappedPoints,
    decimal Weight,
    decimal WeightedPoints,
    string Note);

/// <summary>FR-19: a named, visible deduction from the leading score.</summary>
public sealed record Penalty(string Reason, decimal Points);

/// <summary>FR-20: a named hard veto that forces the final signal to Neutral.</summary>
public sealed record Veto(string Reason);

/// <summary>Per-category regime weights (FR-17).</summary>
public sealed record CategoryWeights(decimal Trend, decimal Momentum, decimal Volatility, decimal Volume)
{
    public decimal For(IndicatorCategory c) => c switch
    {
        IndicatorCategory.Trend => Trend,
        IndicatorCategory.Momentum => Momentum,
        IndicatorCategory.Volatility => Volatility,
        IndicatorCategory.Volume => Volume,
        _ => 1m
    };
}

/// <summary>
/// FR-16/FR-18: the scoring outcome. BuyScore and SellScore are each 0–100 and
/// are computed from independent evidence pools — they are NOT constrained to
/// sum to 100. Penalties and vetoes are carried for full traceability.
/// </summary>
public sealed record ScoreResult(
    decimal BuyScore,
    decimal SellScore,
    MarketRegime Regime,
    CategoryWeights Weights,
    IReadOnlyList<ScoreContribution> Contributions,
    IReadOnlyList<Penalty> Penalties,
    IReadOnlyList<Veto> Vetoes)
{
    public IEnumerable<ScoreContribution> BuyEvidence => Contributions.Where(c => c.Direction == SignalDirection.Buy);
    public IEnumerable<ScoreContribution> SellEvidence => Contributions.Where(c => c.Direction == SignalDirection.Sell);
}
