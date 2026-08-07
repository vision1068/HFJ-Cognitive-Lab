namespace GoldSignalAnalyzer.Presentation;

/// <summary>
/// FR-35: persistence seam for whether the user has acknowledged the first-run
/// disclaimer. Kept an interface so the acknowledgement gate is testable without
/// touching the filesystem, and so a genuine first-run gate can be proven across a
/// fresh store instance (AC-35.3).
/// </summary>
public interface IAcknowledgementStore
{
    bool HasAcknowledged { get; }
    void Acknowledge();
}

/// <summary>In-memory acknowledgement store for tests/headless runs.</summary>
public sealed class InMemoryAcknowledgementStore : IAcknowledgementStore
{
    public bool HasAcknowledged { get; private set; }
    public void Acknowledge() => HasAcknowledged = true;
}

/// <summary>
/// File-backed acknowledgement: the existence of a small marker file means the
/// disclaimer was acknowledged. Durable across launches, so the first-run gate
/// fires exactly once (AC-35.2/AC-35.3). Pure file I/O — no WPF dependency, so it
/// is testable headlessly with a temp path.
/// </summary>
public sealed class FileAcknowledgementStore : IAcknowledgementStore
{
    private readonly string _path;

    public FileAcknowledgementStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Acknowledgement marker path is required.", nameof(path));
        _path = path;
    }

    public bool HasAcknowledged => File.Exists(_path);

    public void Acknowledge()
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(_path, DateTimeOffset.UtcNow.ToString("O"));
    }
}
