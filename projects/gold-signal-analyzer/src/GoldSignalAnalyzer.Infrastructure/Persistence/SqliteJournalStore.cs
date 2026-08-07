using System.Globalization;
using Microsoft.Data.Sqlite;
using GoldSignalAnalyzer.Application.PaperTrading;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Infrastructure.Persistence;

/// <summary>
/// FR-32 (CEO condition C-2): durable <see cref="IJournalStore"/> backed by SQLite.
///
/// This is a paper-trading journal only. It records SIMULATED fills on historical /
/// test candles — nothing here corresponds to, or can trigger, a live order (FR-33 /
/// INV-1). The store performs no network I/O and holds no secret.
///
/// Storage decisions that preserve correctness:
///  - <c>decimal</c> values are stored as invariant-culture TEXT, not REAL. SQLite's
///    only real numeric type is IEEE-754 double, which cannot represent monetary
///    values exactly; TEXT round-trips the decimal bit-for-bit.
///  - enums are stored as INTEGER (their underlying value).
///  - timestamps are stored as round-trip ("O") ISO-8601 TEXT, preserving offset.
///  - insertion order is preserved via the implicit <c>rowid</c>, so <see cref="GetOpen"/>
///    and <see cref="All"/> observe the same ordering as the in-memory store.
/// </summary>
public sealed class SqliteJournalStore : IJournalStore, IDisposable
{
    private readonly SqliteConnection _connection;

    static SqliteJournalStore()
    {
        // Register the e_sqlite3 provider (idempotent). Needed when referencing
        // Microsoft.Data.Sqlite.Core + a bundle rather than the metapackage.
        SQLitePCL.Batteries_V2.Init();
    }

    /// <summary>Opens (creating if absent) a SQLite journal at <paramref name="databasePath"/>.
    /// Use ":memory:" only for ephemeral tests — file paths give durability.</summary>
    public SqliteJournalStore(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Database path is required.", nameof(databasePath));

        _connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString());
        _connection.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS journal_entries (
                id            TEXT    NOT NULL PRIMARY KEY,
                symbol        TEXT    NOT NULL,
                direction     INTEGER NOT NULL,
                status        INTEGER NOT NULL,
                entry_time    TEXT    NOT NULL,
                entry_price   TEXT    NOT NULL,
                stop_loss     TEXT    NOT NULL,
                target        TEXT    NOT NULL,
                size_lots     TEXT    NOT NULL,
                risk_reward   TEXT    NOT NULL,
                regime        INTEGER NOT NULL,
                confidence    TEXT    NOT NULL,
                entry_reason  TEXT    NOT NULL,
                exit_time     TEXT    NULL,
                exit_price    TEXT    NULL,
                exit_reason   TEXT    NULL,
                gross_pnl     TEXT    NULL,
                net_pnl       TEXT    NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public void Add(JournalEntry entry)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO journal_entries
                    (id, symbol, direction, status, entry_time, entry_price, stop_loss,
                     target, size_lots, risk_reward, regime, confidence, entry_reason,
                     exit_time, exit_price, exit_reason, gross_pnl, net_pnl)
                VALUES
                    ($id, $symbol, $direction, $status, $entry_time, $entry_price, $stop_loss,
                     $target, $size_lots, $risk_reward, $regime, $confidence, $entry_reason,
                     $exit_time, $exit_price, $exit_reason, $gross_pnl, $net_pnl);
                """;
            Bind(cmd, entry);
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // constraint (PK) violation
        {
            throw new InvalidOperationException("Entry already exists.", ex);
        }
    }

    public void Update(JournalEntry entry)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            UPDATE journal_entries SET
                symbol=$symbol, direction=$direction, status=$status, entry_time=$entry_time,
                entry_price=$entry_price, stop_loss=$stop_loss, target=$target,
                size_lots=$size_lots, risk_reward=$risk_reward, regime=$regime,
                confidence=$confidence, entry_reason=$entry_reason, exit_time=$exit_time,
                exit_price=$exit_price, exit_reason=$exit_reason, gross_pnl=$gross_pnl,
                net_pnl=$net_pnl
            WHERE id=$id;
            """;
        Bind(cmd, entry);
        var rows = cmd.ExecuteNonQuery();
        if (rows == 0) throw new InvalidOperationException("Entry not found.");
    }

    public JournalEntry? GetOpen()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT * FROM journal_entries
            WHERE status = $open
            ORDER BY rowid ASC
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("$open", (int)PaperTradeStatus.Open);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    public IReadOnlyList<JournalEntry> All()
    {
        var results = new List<JournalEntry>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM journal_entries ORDER BY rowid ASC;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) results.Add(Read(reader));
        return results;
    }

    // ---- mapping ----

    private static void Bind(SqliteCommand cmd, JournalEntry e)
    {
        cmd.Parameters.AddWithValue("$id", e.Id.ToString());
        cmd.Parameters.AddWithValue("$symbol", e.Symbol.Value);
        cmd.Parameters.AddWithValue("$direction", (int)e.Direction);
        cmd.Parameters.AddWithValue("$status", (int)e.Status);
        cmd.Parameters.AddWithValue("$entry_time", Iso(e.EntryTimeUtc));
        cmd.Parameters.AddWithValue("$entry_price", Dec(e.EntryPrice));
        cmd.Parameters.AddWithValue("$stop_loss", Dec(e.StopLoss));
        cmd.Parameters.AddWithValue("$target", Dec(e.Target));
        cmd.Parameters.AddWithValue("$size_lots", Dec(e.SizeLots));
        cmd.Parameters.AddWithValue("$risk_reward", Dec(e.RiskReward));
        cmd.Parameters.AddWithValue("$regime", (int)e.Regime);
        cmd.Parameters.AddWithValue("$confidence", Dec(e.Confidence));
        cmd.Parameters.AddWithValue("$entry_reason", e.EntryReason);
        cmd.Parameters.AddWithValue("$exit_time", e.ExitTimeUtc is { } et ? Iso(et) : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$exit_price", e.ExitPrice is { } ep ? Dec(ep) : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$exit_reason", (object?)e.ExitReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$gross_pnl", e.GrossPnl is { } gp ? Dec(gp) : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$net_pnl", e.NetPnl is { } np ? Dec(np) : (object)DBNull.Value);
    }

    private static JournalEntry Read(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(r.GetOrdinal("id"))),
        Symbol = new NormalizedSymbol(r.GetString(r.GetOrdinal("symbol"))),
        Direction = (SignalDirection)r.GetInt32(r.GetOrdinal("direction")),
        Status = (PaperTradeStatus)r.GetInt32(r.GetOrdinal("status")),
        EntryTimeUtc = ReadIso(r, "entry_time")!.Value,
        EntryPrice = ReadDec(r, "entry_price")!.Value,
        StopLoss = ReadDec(r, "stop_loss")!.Value,
        Target = ReadDec(r, "target")!.Value,
        SizeLots = ReadDec(r, "size_lots")!.Value,
        RiskReward = ReadDec(r, "risk_reward")!.Value,
        Regime = (MarketRegime)r.GetInt32(r.GetOrdinal("regime")),
        Confidence = ReadDec(r, "confidence")!.Value,
        EntryReason = r.GetString(r.GetOrdinal("entry_reason")),
        ExitTimeUtc = ReadIso(r, "exit_time"),
        ExitPrice = ReadDec(r, "exit_price"),
        ExitReason = ReadNullableString(r, "exit_reason"),
        GrossPnl = ReadDec(r, "gross_pnl"),
        NetPnl = ReadDec(r, "net_pnl"),
    };

    private static string Dec(decimal d) => d.ToString(CultureInfo.InvariantCulture);
    private static string Iso(DateTimeOffset t) => t.ToString("O", CultureInfo.InvariantCulture);

    private static decimal? ReadDec(SqliteDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : decimal.Parse(r.GetString(i), CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset? ReadIso(SqliteDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i)
            ? null
            : DateTimeOffset.Parse(r.GetString(i), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
    }

    private static string? ReadNullableString(SqliteDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    public void Dispose() => _connection.Dispose();
}
