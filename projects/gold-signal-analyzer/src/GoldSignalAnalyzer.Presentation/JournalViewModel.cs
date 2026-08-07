using System.Collections.ObjectModel;
using System.Globalization;
using GoldSignalAnalyzer.Application.PaperTrading;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Presentation.Mvvm;

namespace GoldSignalAnalyzer.Presentation;

/// <summary>One read-only journal row. Renders stored values only — an open trade
/// shows blank exit/P&amp;L rather than a guessed one (INV-5, AC-26.2c).</summary>
public sealed class JournalRowViewModel
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public JournalRowViewModel(JournalEntry e)
    {
        if (e is null) throw new ArgumentNullException(nameof(e));
        Symbol = e.Symbol.Value;
        Direction = e.Direction.ToString().ToUpperInvariant();
        Status = e.Status.ToString();
        EntryTime = e.EntryTimeUtc.ToString("u", Inv);
        EntryPrice = e.EntryPrice.ToString("0.##", Inv);
        SizeLots = e.SizeLots.ToString("0.##", Inv);
        RiskReward = e.RiskReward.ToString("0.##", Inv);
        Regime = e.Regime.ToString();
        Confidence = e.Confidence.ToString("0.#", Inv);
        ExitTime = e.ExitTimeUtc?.ToString("u", Inv) ?? "";
        ExitPrice = e.ExitPrice?.ToString("0.##", Inv) ?? "";
        NetPnl = e.NetPnl?.ToString("0.##", Inv) ?? "";
        GrossPnl = e.GrossPnl?.ToString("0.##", Inv) ?? "";
        EntryReason = e.EntryReason;
    }

    public string Symbol { get; }
    public string Direction { get; }
    public string Status { get; }
    public string EntryTime { get; }
    public string EntryPrice { get; }
    public string SizeLots { get; }
    public string RiskReward { get; }
    public string Regime { get; }
    public string Confidence { get; }
    public string ExitTime { get; }
    public string ExitPrice { get; }
    public string NetPnl { get; }
    public string GrossPnl { get; }
    public string EntryReason { get; }
}

/// <summary>
/// FR-26.2: a DISPLAY-ONLY view over the paper-trading journal. It reads from
/// <see cref="IJournalStore"/> and exposes NO open/close/modify command — the shell
/// cannot execute or simulate a trade (INV-1). The realised-P&amp;L total sums stored
/// Net P&amp;L over CLOSED trades only.
/// </summary>
public sealed class JournalViewModel : ViewModelBase
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private readonly IJournalStore _store;

    public JournalViewModel(IJournalStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        Refresh();
    }

    public ObservableCollection<JournalRowViewModel> Rows { get; } = new();

    public int ClosedCount => _store.All().Count(e => e.Status == PaperTradeStatus.Closed);
    public int OpenCount => _store.All().Count(e => e.Status == PaperTradeStatus.Open);

    /// <summary>Realised Net P&amp;L over closed trades only (AC-26.2d).</summary>
    public string RealisedNetPnlLabel
    {
        get
        {
            decimal sum = _store.All()
                .Where(e => e.Status == PaperTradeStatus.Closed && e.NetPnl is not null)
                .Sum(e => e.NetPnl!.Value);
            return $"Realised Net P&L (closed only): {sum.ToString("0.##", Inv)}";
        }
    }

    /// <summary>Reloads rows from the store. There is no mutation path here.</summary>
    public void Refresh()
    {
        Rows.Clear();
        foreach (var e in _store.All()) Rows.Add(new JournalRowViewModel(e));
        OnPropertyChanged(nameof(RealisedNetPnlLabel));
        OnPropertyChanged(nameof(ClosedCount));
        OnPropertyChanged(nameof(OpenCount));
    }
}
