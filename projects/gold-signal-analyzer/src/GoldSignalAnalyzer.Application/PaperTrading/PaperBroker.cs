using GoldSignalAnalyzer.Application.Risk;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.PaperTrading;

/// <summary>Outcome of a paper open/close request.</summary>
public sealed record PaperActionResult(bool Ok, string Message, JournalEntry? Entry);

/// <summary>
/// FR-32/FR-33: simulates trade execution against historical/test candle prices
/// and records everything to the journal. It enforces the single-open-position
/// rule (AC-32.3). Crucially it depends ONLY on a <see cref="SymbolSpec"/> and an
/// <see cref="IJournalStore"/> — it has no reference to any bridge/order client,
/// so there is structurally no path from a paper fill to a live order (INV-1).
/// A guard test asserts this by reflection.
/// </summary>
public sealed class PaperBroker
{
    private readonly IJournalStore _store;
    private readonly SymbolSpec _spec;
    private readonly decimal _costPerTrade;

    public PaperBroker(IJournalStore store, SymbolSpec spec, decimal costPerTrade = 0m)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        if (costPerTrade < 0) throw new ArgumentOutOfRangeException(nameof(costPerTrade));
        _costPerTrade = costPerTrade;
    }

    public PaperActionResult Open(
        RiskPlan plan, DateTimeOffset timeUtc, MarketRegime regime, decimal confidence, string entryReason)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        if (!plan.IsTradeable)
            return new PaperActionResult(false, $"Risk plan not tradeable: {plan.RejectReason}", null);
        if (_store.GetOpen() is not null)
            return new PaperActionResult(false, "A position is already open — only one open position permitted.", null);

        var entry = new JournalEntry
        {
            Symbol = _spec.Symbol,
            Direction = plan.Direction,
            Status = PaperTradeStatus.Open,
            EntryTimeUtc = timeUtc.ToUniversalTime(),
            EntryPrice = plan.Entry,
            StopLoss = plan.StopLoss,
            Target = plan.Target,
            SizeLots = plan.PositionSizeLots,
            RiskReward = plan.RiskReward,
            Regime = regime,
            Confidence = confidence,
            EntryReason = entryReason,
        };
        _store.Add(entry);
        return new PaperActionResult(true, "Opened.", entry);
    }

    public PaperActionResult Close(decimal exitPrice, DateTimeOffset timeUtc, string exitReason)
    {
        if (exitPrice <= 0) throw new ArgumentOutOfRangeException(nameof(exitPrice));
        var open = _store.GetOpen();
        if (open is null) return new PaperActionResult(false, "No open position to close.", null);

        int sign = open.Direction == SignalDirection.Buy ? 1 : -1;
        decimal gross = (exitPrice - open.EntryPrice) * sign * _spec.ContractSize * open.SizeLots;
        decimal net = gross - _costPerTrade;

        var closed = open with
        {
            Status = PaperTradeStatus.Closed,
            ExitTimeUtc = timeUtc.ToUniversalTime(),
            ExitPrice = exitPrice,
            ExitReason = exitReason,
            GrossPnl = Math.Round(gross, 2),
            NetPnl = Math.Round(net, 2),
        };
        _store.Update(closed);
        return new PaperActionResult(true, "Closed.", closed);
    }
}
