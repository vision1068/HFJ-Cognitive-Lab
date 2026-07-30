using GoldSignalAnalyzer.Application.PaperTrading;
using GoldSignalAnalyzer.Application.Risk;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Infrastructure.Persistence;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

// FR-32 / CEO condition C-2 — the durable SQLite journal adapter.
// Every test that claims "durable" reopens a SECOND store on the SAME file
// (a fresh connection) and reads back, so it proves persistence to disk and
// not just an in-process cache.
public class SqliteJournalStoreTests : IDisposable
{
    private readonly List<string> _temps = new();
    private static readonly DateTimeOffset T0 = new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    private string NewDbPath()
    {
        var p = Path.Combine(Path.GetTempPath(), $"gsa-journal-{Guid.NewGuid():N}.db");
        _temps.Add(p);
        return p;
    }

    private static JournalEntry OpenEntry() => new()
    {
        Symbol = NormalizedSymbol.Gold,
        Direction = SignalDirection.Buy,
        Status = PaperTradeStatus.Open,
        EntryTimeUtc = T0,
        // deliberately many-digit decimals: TEXT storage must round-trip these exactly,
        // which REAL (double) storage could not.
        EntryPrice = 1993.12345678m,
        StopLoss = 1985.5m,
        Target = 2030.25m,
        SizeLots = 0.03m,
        RiskReward = 2.0m,
        Regime = MarketRegime.TrendingUp,
        Confidence = 72.5m,
        EntryReason = "BUY breakout; RSI cross",
    };

    // ---- AC-C2.1: durable round-trip across a fresh connection ----

    [Fact]
    public void Entry_survives_store_reopen_with_every_field_intact()
    {
        var path = NewDbPath();
        var entry = OpenEntry();

        using (var writer = new SqliteJournalStore(path))
            writer.Add(entry);

        using var reader = new SqliteJournalStore(path); // new connection, same file
        var got = Assert.Single(reader.All());

        Assert.Equal(entry.Id, got.Id);
        Assert.Equal(entry.Symbol.Value, got.Symbol.Value);
        Assert.Equal(entry.Direction, got.Direction);
        Assert.Equal(entry.Status, got.Status);
        Assert.Equal(entry.EntryTimeUtc, got.EntryTimeUtc);
        Assert.Equal(1993.12345678m, got.EntryPrice); // exact — proves TEXT, not REAL
        Assert.Equal(entry.StopLoss, got.StopLoss);
        Assert.Equal(entry.Target, got.Target);
        Assert.Equal(entry.SizeLots, got.SizeLots);
        Assert.Equal(entry.RiskReward, got.RiskReward);
        Assert.Equal(entry.Regime, got.Regime);
        Assert.Equal(entry.Confidence, got.Confidence);
        Assert.Equal(entry.EntryReason, got.EntryReason);
        // open entry has null exit fields
        Assert.Null(got.ExitTimeUtc);
        Assert.Null(got.ExitPrice);
        Assert.Null(got.ExitReason);
        Assert.Null(got.NetPnl);
    }

    // ---- AC-C2.2: an Update (close) persists across reopen; GetOpen reflects it ----

    [Fact]
    public void Close_via_update_persists_and_clears_open_position()
    {
        var path = NewDbPath();
        var entry = OpenEntry();

        using (var writer = new SqliteJournalStore(path))
        {
            writer.Add(entry);
            Assert.NotNull(writer.GetOpen());
            var closed = entry with
            {
                Status = PaperTradeStatus.Closed,
                ExitTimeUtc = T0.AddHours(3),
                ExitPrice = 2010.75m,
                ExitReason = "target hit",
                GrossPnl = 52.65m,
                NetPnl = 50.10m,
            };
            writer.Update(closed);
        }

        using var reader = new SqliteJournalStore(path);
        Assert.Null(reader.GetOpen()); // no open position survives
        var got = Assert.Single(reader.All());
        Assert.Equal(PaperTradeStatus.Closed, got.Status);
        Assert.Equal(T0.AddHours(3), got.ExitTimeUtc);
        Assert.Equal(2010.75m, got.ExitPrice);
        Assert.Equal("target hit", got.ExitReason);
        Assert.Equal(50.10m, got.NetPnl);
    }

    // ---- AC-C2.3: insertion order preserved; GetOpen returns earliest open ----

    [Fact]
    public void All_preserves_insertion_order_and_GetOpen_returns_the_open_one()
    {
        var path = NewDbPath();
        var first = OpenEntry() with
        {
            Status = PaperTradeStatus.Closed,
            ExitTimeUtc = T0.AddHours(1),
            ExitPrice = 2005m,
            ExitReason = "closed",
            NetPnl = 10m,
        };
        var second = OpenEntry() with { EntryTimeUtc = T0.AddHours(2), EntryReason = "second" };

        using (var writer = new SqliteJournalStore(path))
        {
            writer.Add(first);
            writer.Add(second);
        }

        using var reader = new SqliteJournalStore(path);
        var all = reader.All();
        Assert.Equal(2, all.Count);
        Assert.Equal(first.Id, all[0].Id);   // insertion order held
        Assert.Equal(second.Id, all[1].Id);
        Assert.Equal(second.Id, reader.GetOpen()!.Id); // the only open one
    }

    // ---- guard behaviour matches the in-memory store's contract ----

    [Fact]
    public void Duplicate_add_throws()
    {
        var path = NewDbPath();
        using var store = new SqliteJournalStore(path);
        var e = OpenEntry();
        store.Add(e);
        var ex = Assert.Throws<InvalidOperationException>(() => store.Add(e));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public void Update_of_missing_entry_throws()
    {
        var path = NewDbPath();
        using var store = new SqliteJournalStore(path);
        var ex = Assert.Throws<InvalidOperationException>(() => store.Update(OpenEntry()));
        Assert.Contains("not found", ex.Message);
    }

    // ---- FR-32 end-to-end: PaperBroker drives the SQLite store, journal is durable ----

    [Fact]
    public void PaperBroker_over_sqlite_persists_a_completed_trade()
    {
        var path = NewDbPath();
        var spec = SymbolSpec.Gold();
        var plan = new RiskPlanCalculator()
            .Build(SignalDirection.Buy, 2000m, 10m, spec, 10_000m, hasOpenPosition: false);

        using (var store = new SqliteJournalStore(path))
        {
            var broker = new PaperBroker(store, spec);
            Assert.True(broker.Open(plan, T0, MarketRegime.TrendingUp, 72m, "entry").Ok);
            Assert.True(broker.Close(2010m, T0.AddHours(3), "target hit").Ok);
        }

        using var reopened = new SqliteJournalStore(path);
        var got = Assert.Single(reopened.All());
        Assert.Equal(PaperTradeStatus.Closed, got.Status);
        Assert.Equal(30m, got.NetPnl); // (2010-2000)*100*0.03 — matches in-memory PaperBroker test
        Assert.Null(reopened.GetOpen());
    }

    public void Dispose()
    {
        foreach (var p in _temps)
            try { if (File.Exists(p)) File.Delete(p); } catch { /* best-effort temp cleanup */ }
    }
}
