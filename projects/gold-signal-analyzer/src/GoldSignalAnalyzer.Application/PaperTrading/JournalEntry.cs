using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.PaperTrading;

/// <summary>
/// FR-32 (AC-32.2): a full paper-trading journal record. Captures the entry, the
/// risk plan it came from, the market context (regime, confidence, reason), and
/// — once closed — the exit and realised P&L. These are simulated fills on
/// historical/test candles; no field can ever correspond to a live order (FR-33).
/// </summary>
public sealed record JournalEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public NormalizedSymbol Symbol { get; init; } = NormalizedSymbol.Gold;
    public SignalDirection Direction { get; init; }
    public PaperTradeStatus Status { get; init; } = PaperTradeStatus.Open;

    public DateTimeOffset EntryTimeUtc { get; init; }
    public decimal EntryPrice { get; init; }
    public decimal StopLoss { get; init; }
    public decimal Target { get; init; }
    public decimal SizeLots { get; init; }
    public decimal RiskReward { get; init; }
    public MarketRegime Regime { get; init; }
    public decimal Confidence { get; init; }
    public string EntryReason { get; init; } = "";

    public DateTimeOffset? ExitTimeUtc { get; init; }
    public decimal? ExitPrice { get; init; }
    public string? ExitReason { get; init; }
    public decimal? GrossPnl { get; init; }
    public decimal? NetPnl { get; init; }
}
