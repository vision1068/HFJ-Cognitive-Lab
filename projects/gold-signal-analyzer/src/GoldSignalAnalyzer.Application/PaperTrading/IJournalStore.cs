using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.PaperTrading;

/// <summary>
/// FR-32: persistence seam for the paper-trading journal. The production adapter
/// is a SQLite/EF Core store; this interface keeps the paper engine testable with
/// an in-memory store and free of any I/O dependency.
/// </summary>
public interface IJournalStore
{
    void Add(JournalEntry entry);
    void Update(JournalEntry entry);
    JournalEntry? GetOpen();
    IReadOnlyList<JournalEntry> All();
}

/// <summary>In-memory <see cref="IJournalStore"/> for tests and headless runs.</summary>
public sealed class InMemoryJournalStore : IJournalStore
{
    private readonly Dictionary<Guid, JournalEntry> _entries = new();
    private readonly List<Guid> _order = new();

    public void Add(JournalEntry entry)
    {
        if (_entries.ContainsKey(entry.Id)) throw new InvalidOperationException("Entry already exists.");
        _entries[entry.Id] = entry;
        _order.Add(entry.Id);
    }

    public void Update(JournalEntry entry)
    {
        if (!_entries.ContainsKey(entry.Id)) throw new InvalidOperationException("Entry not found.");
        _entries[entry.Id] = entry;
    }

    public JournalEntry? GetOpen()
        => _order.Select(id => _entries[id]).FirstOrDefault(e => e.Status == PaperTradeStatus.Open);

    public IReadOnlyList<JournalEntry> All() => _order.Select(id => _entries[id]).ToList();
}
