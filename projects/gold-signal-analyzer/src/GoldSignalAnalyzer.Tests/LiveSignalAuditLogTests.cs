using GoldSignalAnalyzer.Application.Analysis;
using GoldSignalAnalyzer.Application.Freshness;
using GoldSignalAnalyzer.Application.Live;
using GoldSignalAnalyzer.Application.Scoring;
using GoldSignalAnalyzer.Domain;
using GoldSignalAnalyzer.Presentation;
using GoldSignalAnalyzer.Testing;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// Cycle 7 (FR-38) — the live-signal audit trail. The load-bearing properties:
///   * a SUPPRESSED refresh is recorded with NO numeric fields (no fabricated score
///     ever enters the trail — INV-4/NFR-5) and the exact suppression reason;
///   * an ALLOWED refresh records exactly what was displayed (direction/scores/confidence);
///   * the file log is APPEND-ONLY and durable (a fresh reader on the same file reads
///     every prior entry back, in order);
///   * the persisted lines carry NO secret (INV-2/INV-3);
///   * the audit types expose NO order/execution affordance (INV-1).
/// </summary>
public class LiveSignalAuditLogTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
    private readonly List<string> _temps = new();

    private string NewPath()
    {
        var p = Path.Combine(Path.GetTempPath(), $"gsa-audit-{Guid.NewGuid():N}.jsonl");
        _temps.Add(p);
        return p;
    }

    private static IReadOnlyList<Candle> RisingCandles(int n = 80)
    {
        var list = new List<Candle>();
        var start = Now.AddHours(-(n - 1));
        decimal basePrice = 1900m;
        for (int i = 0; i < n; i++)
        {
            decimal open = basePrice + i * 1.5m;
            list.Add(new Candle(NormalizedSymbol.Gold, TimeFrame.H1, start.AddHours(i),
                open, open + 1.5m, open - 0.5m, open + 1.0m, volume: 1000 + i));
        }
        return list;
    }

    private static LiveRefreshResult AllowedResult()
    {
        var clock = new ManualClock(Now);
        var provider = new TestMarketDataProvider()
            .WithSymbols(new BrokerSymbol("XAUUSD", "Gold"))
            .WithCandles(RisingCandles())
            .WithLatestTick(new MarketTick(NormalizedSymbol.Gold, 1990m, 1990.2m, Now));
        provider.ConnectAsync().GetAwaiter().GetResult();
        var coord = new LiveSignalCoordinator(
            provider, new SignalAnalysisService(clock), new SignalGate(),
            new DataFreshnessMonitor(clock, TimeSpan.FromMinutes(90)),
            new LiveSignalOptions(NormalizedSymbol.Gold, TimeFrame.H1, 80, SymbolSpec.Gold(),
                10_000m, new SignalContext(HtfDirection: SignalDirection.Buy)),
            hasSymbolMapping: true, clock);
        return coord.RefreshAsync(hasOpenPosition: false).GetAwaiter().GetResult();
    }

    private static LiveRefreshResult SuppressedResult(string reason, ConnectionState state) => new(
        new SignalGateDecision(SignalGateStatus.Suppressed, reason),
        Analysis: null,
        Candles: Array.Empty<Candle>(),
        Freshness: new FreshnessAssessment(FreshnessStatus.Stale, TimeSpan.FromHours(3), null),
        ConnectionState: state);

    [Fact] // A suppressed refresh records the reason and NO fabricated numbers.
    public void Suppressed_entry_has_reason_and_no_numeric_fields()
    {
        var r = SuppressedResult(SignalGate.StaleReason, ConnectionState.Connected);
        var e = LiveSignalAuditEntry.From(r, NormalizedSymbol.Gold, TimeFrame.H1, Now);

        Assert.False(e.SignalAllowed);
        Assert.Equal(SignalGate.StaleReason, e.SuppressionReason);
        Assert.Null(e.Direction);
        Assert.Null(e.BuyScore);
        Assert.Null(e.SellScore);
        Assert.Null(e.Confidence);
        Assert.Null(e.IsActionable);
        Assert.Null(e.DisplayedReason);
        Assert.Equal("XAU/USD", e.Symbol);
        Assert.Equal("Stale", e.FreshnessStatus);
    }

    [Fact] // An allowed refresh records exactly what was displayed.
    public void Allowed_entry_captures_displayed_classification()
    {
        var r = AllowedResult();
        Assert.True(r.SignalAllowed); // guard the fixture

        var e = LiveSignalAuditEntry.From(r, NormalizedSymbol.Gold, TimeFrame.H1, Now);

        Assert.True(e.SignalAllowed);
        Assert.Null(e.SuppressionReason);
        Assert.Equal(r.Analysis!.Signal.Direction.ToString(), e.Direction);
        Assert.Equal(r.Analysis.Signal.BuyScore, e.BuyScore);
        Assert.Equal(r.Analysis.Signal.SellScore, e.SellScore);
        Assert.Equal(r.Analysis.Signal.Confidence, e.Confidence);
        Assert.Equal(r.Analysis.Signal.IsActionable, e.IsActionable);
        Assert.Equal("Fresh", e.FreshnessStatus);
    }

    [Fact] // InMemory log: Recent returns the most recent N, oldest-first.
    public void InMemory_recent_returns_last_n_in_order()
    {
        var log = new InMemoryLiveSignalAuditLog();
        for (int i = 0; i < 5; i++)
            log.Record(LiveSignalAuditEntry.From(
                SuppressedResult($"R{i}", ConnectionState.Disconnected),
                NormalizedSymbol.Gold, TimeFrame.H1, Now.AddMinutes(i)));

        var recent = log.Recent(3);
        Assert.Equal(5, log.Count);
        Assert.Equal(3, recent.Count);
        Assert.Equal("R2", recent[0].SuppressionReason);
        Assert.Equal("R4", recent[2].SuppressionReason);
    }

    [Fact] // FR-38: the file log is append-only and durable — a fresh reader reads prior entries.
    public void File_log_is_append_only_and_survives_reopen()
    {
        var path = NewPath();
        new JsonlFileLiveSignalAuditLog(path).Record(LiveSignalAuditEntry.From(
            SuppressedResult(SignalGate.NotConnectedReason, ConnectionState.Disconnected),
            NormalizedSymbol.Gold, TimeFrame.H1, Now));

        // A SECOND, fresh writer appends — it must NOT erase the first entry.
        new JsonlFileLiveSignalAuditLog(path).Record(
            LiveSignalAuditEntry.From(AllowedResult(), NormalizedSymbol.Gold, TimeFrame.H1, Now.AddMinutes(5)));

        var back = new JsonlFileLiveSignalAuditLog(path).Recent(10);
        Assert.Equal(2, back.Count);
        Assert.False(back[0].SignalAllowed);                                   // first, preserved
        Assert.Equal(SignalGate.NotConnectedReason, back[0].SuppressionReason);
        Assert.True(back[1].SignalAllowed);                                    // second, appended
    }

    [Fact] // INV-2/INV-3: the persisted audit lines carry no secret-shaped token.
    public void Persisted_audit_json_contains_no_secret_token()
    {
        var path = NewPath();
        var log = new JsonlFileLiveSignalAuditLog(path);
        log.Record(LiveSignalAuditEntry.From(AllowedResult(), NormalizedSymbol.Gold, TimeFrame.H1, Now));
        log.Record(LiveSignalAuditEntry.From(
            SuppressedResult(SignalGate.StaleReason, ConnectionState.Connected),
            NormalizedSymbol.Gold, TimeFrame.H1, Now));

        var json = File.ReadAllText(path).ToLowerInvariant();
        foreach (var forbidden in new[] { "password", "passwd", "pwd", "secret", "investor", "pin", "token", "apikey", "otp" })
            Assert.DoesNotContain(forbidden, json);
    }

    [Fact] // INV-1: the audit types expose NO order/execution affordance.
    public void Audit_types_expose_no_execution_member()
    {
        // NB: "buy"/"sell" are intentionally NOT forbidden here — BuyScore/SellScore are
        // recorded DISPLAY values (mirroring SignalClassification), not order verbs. This
        // guard targets true order/execution verbs only.
        foreach (var t in new[]
        {
            typeof(LiveSignalAuditEntry), typeof(ILiveSignalAuditLog),
            typeof(InMemoryLiveSignalAuditLog), typeof(JsonlFileLiveSignalAuditLog),
        })
        {
            var members = t.GetMembers().Select(m => m.Name.ToLowerInvariant()).ToList();
            foreach (var forbidden in new[] { "execute", "submit", "order", "place", "cancel", "modify", "sendorder" })
                Assert.DoesNotContain(members, m => m.Contains(forbidden));
        }
    }

    public void Dispose()
    {
        foreach (var p in _temps)
            try { if (File.Exists(p)) File.Delete(p); } catch { /* best-effort temp cleanup */ }
    }
}
