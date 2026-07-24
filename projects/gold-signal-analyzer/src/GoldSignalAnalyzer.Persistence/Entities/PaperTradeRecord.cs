namespace GoldSignalAnalyzer.Persistence.Entities;

/// <summary>Schema-stub journal row (arch §6, FR-32). Simulated only — never a real order.</summary>
public sealed class PaperTradeRecord : AuditableEntity
{
    public DateTime OpenedUtc { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal SimulatedEntry { get; set; }
    public decimal SimulatedStopLoss { get; set; }
    public decimal SimulatedTarget { get; set; }
}
