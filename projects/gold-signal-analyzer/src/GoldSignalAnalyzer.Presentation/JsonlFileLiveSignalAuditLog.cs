using System.IO;
using System.Text.Json;
using GoldSignalAnalyzer.Application.Live;

namespace GoldSignalAnalyzer.Presentation;

/// <summary>
/// FR-38: file-backed live-signal audit trail using in-framework <c>System.Text.Json</c>
/// (no new package). Each entry is appended as ONE JSON object on its own line (JSON Lines),
/// so the trail is append-only and durable across launches — a paused/rolled dashboard can
/// never erase what was previously shown. Pure file I/O, no WPF dependency, so it is testable
/// headlessly with a temp path — same shape as <see cref="FileAcknowledgementStore"/> and
/// <c>JsonFileSetupProfileStore</c>.
///
/// The persisted lines carry no secret (the entry type has no secret member, INV-2/INV-3) and
/// the writer only ever appends — it exposes no order/execution affordance (INV-1).
/// </summary>
public sealed class JsonlFileLiveSignalAuditLog : ILiveSignalAuditLog
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    private readonly string _path;

    public JsonlFileLiveSignalAuditLog(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Audit log path is required.", nameof(path));
        _path = path;
    }

    public void Record(LiveSignalAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var line = JsonSerializer.Serialize(entry, Options);
        File.AppendAllText(_path, line + Environment.NewLine);
    }

    public IReadOnlyList<LiveSignalAuditEntry> Recent(int max)
    {
        if (max <= 0 || !File.Exists(_path)) return Array.Empty<LiveSignalAuditEntry>();
        var lines = File.ReadAllLines(_path);
        var result = new List<LiveSignalAuditEntry>();
        int start = Math.Max(0, lines.Length - max);
        for (int i = start; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var e = JsonSerializer.Deserialize<LiveSignalAuditEntry>(lines[i], Options);
            if (e is not null) result.Add(e);
        }
        return result;
    }
}
