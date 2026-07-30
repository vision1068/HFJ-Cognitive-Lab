namespace GoldSignalAnalyzer.Domain;

/// <summary>Lifecycle of a simulated (paper) trade. There is no live counterpart —
/// this app has no order-submission path at all (INV-1 / FR-33).</summary>
public enum PaperTradeStatus
{
    Open = 0,
    Closed = 1
}
