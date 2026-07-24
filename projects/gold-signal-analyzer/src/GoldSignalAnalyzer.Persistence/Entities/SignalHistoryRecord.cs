using GoldSignalAnalyzer.Domain.Enums;

namespace GoldSignalAnalyzer.Persistence.Entities;

/// <summary>Schema-stub row for a future emitted signal (arch §6). No financial-secret columns.</summary>
public sealed class SignalHistoryRecord : AuditableEntity
{
    public DateTime TimestampUtc { get; set; }
    public SignalDirection Direction { get; set; }
    public int BuyScore { get; set; }
    public int SellScore { get; set; }
    public int Confidence { get; set; }
}
