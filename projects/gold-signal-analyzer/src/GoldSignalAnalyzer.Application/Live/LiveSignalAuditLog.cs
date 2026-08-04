using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Live;

/// <summary>
/// Cycle 7 (FR-38): an append-only audit record of exactly what the LIVE advisory
/// surface showed the user, and when. Because the live feed renders an actionable
/// BUY/SELL classification on a real broker account's instrument, governance requires
/// a durable trail of what was displayed (or why a signal was suppressed) at each
/// refresh — independent of the volatile on-screen state.
///
/// The record honours the no-fabrication invariant (INV-4/NFR-5): when the gate
/// SUPPRESSED a signal, the numeric fields are null and only the suppression reason is
/// recorded — an audit entry can never carry an invented score. It carries NO secret
/// (INV-2/INV-3) and NO order/execution affordance (INV-1): it is a passive data record.
/// </summary>
public sealed record LiveSignalAuditEntry(
    string RecordedAtUtc,
    string Symbol,
    string TimeFrame,
    string ConnectionState,
    string FreshnessStatus,
    bool SignalAllowed,
    string? Direction,
    bool? IsActionable,
    decimal? BuyScore,
    decimal? SellScore,
    decimal? Confidence,
    string? DisplayedReason,
    string? SuppressionReason)
{
    /// <summary>
    /// Build an audit entry from one live refresh outcome. When the gate allowed a
    /// signal the displayed classification (direction/scores/confidence/reason) is
    /// captured verbatim; when it suppressed one, only the banner reason is captured and
    /// every numeric field stays null (no fabricated value ever enters the trail).
    /// </summary>
    public static LiveSignalAuditEntry From(
        LiveRefreshResult result,
        NormalizedSymbol symbol,
        TimeFrame timeFrame,
        DateTimeOffset recordedAtUtc)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        var a = result.Analysis;
        bool allowed = result.SignalAllowed;
        return new LiveSignalAuditEntry(
            RecordedAtUtc: recordedAtUtc.ToString("O"),
            Symbol: symbol.Value,
            TimeFrame: timeFrame.ToString(),
            ConnectionState: result.ConnectionState.ToString(),
            FreshnessStatus: result.Freshness.Status.ToString(),
            SignalAllowed: allowed,
            Direction: allowed ? a!.Signal.Direction.ToString() : null,
            IsActionable: allowed ? a!.Signal.IsActionable : null,
            BuyScore: allowed ? a!.Signal.BuyScore : null,
            SellScore: allowed ? a!.Signal.SellScore : null,
            Confidence: allowed ? a!.Signal.Confidence : null,
            DisplayedReason: allowed ? a!.Signal.PrimaryReason : null,
            SuppressionReason: allowed ? null : result.SuppressionReason);
    }
}

/// <summary>
/// FR-38: append-only persistence seam for the live-signal audit trail. Kept an
/// interface so the recording is testable without touching the filesystem, mirroring
/// the <c>ISetupProfileStore</c>/<c>IAcknowledgementStore</c> precedent. It exposes only
/// Record (append) and Recent (read-back) — no mutation, no deletion, and deliberately
/// NO order/execution member (INV-1).
/// </summary>
public interface ILiveSignalAuditLog
{
    /// <summary>Append one entry to the trail (durably, in an append-only fashion).</summary>
    void Record(LiveSignalAuditEntry entry);

    /// <summary>The most recent <paramref name="max"/> entries, oldest-first.</summary>
    IReadOnlyList<LiveSignalAuditEntry> Recent(int max);
}

/// <summary>In-memory audit log for tests/headless runs.</summary>
public sealed class InMemoryLiveSignalAuditLog : ILiveSignalAuditLog
{
    private readonly List<LiveSignalAuditEntry> _entries = new();

    public void Record(LiveSignalAuditEntry entry)
        => _entries.Add(entry ?? throw new ArgumentNullException(nameof(entry)));

    public IReadOnlyList<LiveSignalAuditEntry> Recent(int max)
    {
        if (max <= 0) return Array.Empty<LiveSignalAuditEntry>();
        int skip = Math.Max(0, _entries.Count - max);
        return _entries.Skip(skip).ToList();
    }

    /// <summary>Total number of recorded entries (test/inspection helper).</summary>
    public int Count => _entries.Count;
}
