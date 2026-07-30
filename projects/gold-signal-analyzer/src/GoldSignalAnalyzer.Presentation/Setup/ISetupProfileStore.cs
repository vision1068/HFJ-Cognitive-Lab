using System.Text.Json;

namespace GoldSignalAnalyzer.Presentation.Setup;

/// <summary>
/// FR-28 / AC-28.7: persistence seam for the first-run setup profile. Kept an
/// interface so the setup gate is testable without touching the filesystem, and so a
/// genuine first-run gate can be proven across a FRESH store instance on the same file
/// (durability, NFR-SETUP-3). Mirrors <see cref="IAcknowledgementStore"/> file-for-file
/// — the closest structural precedent (a first-run gate backed by a tiny persisted file),
/// and it deliberately lives in the net8.0 Presentation project (no WPF ref) so the
/// existing test project exercises it headlessly.
/// </summary>
public interface ISetupProfileStore
{
    /// <summary>True once a profile has been saved. <c>NeedsSetup</c> == <c>!HasProfile</c> (AC-28.7).</summary>
    bool HasProfile { get; }

    /// <summary>The saved profile, or null on a true first run.</summary>
    SetupProfile? Load();

    /// <summary>Persists the profile durably (overwriting any prior one).</summary>
    void Save(SetupProfile profile);
}

/// <summary>In-memory setup-profile store for tests/headless runs.</summary>
public sealed class InMemorySetupProfileStore : ISetupProfileStore
{
    private SetupProfile? _profile;

    public bool HasProfile => _profile is not null;

    public SetupProfile? Load() => _profile;

    public void Save(SetupProfile profile)
        => _profile = profile ?? throw new ArgumentNullException(nameof(profile));
}

/// <summary>
/// JSON-file-backed setup profile using in-framework <c>System.Text.Json</c> (no new
/// package — D5-4/NFR-SETUP-3). The existence of the file means setup is complete, so
/// the first-run gate fires exactly once (AC-28.7). Pure file I/O — no WPF dependency,
/// testable headlessly with a temp path — same shape as <see cref="FileAcknowledgementStore"/>.
/// Durability is real: a second, fresh store on the same path reads back exact values,
/// including the exact <see cref="SetupProfile.AccountBalance"/> decimal.
/// </summary>
public sealed class JsonFileSetupProfileStore : ISetupProfileStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _path;

    public JsonFileSetupProfileStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Setup profile path is required.", nameof(path));
        _path = path;
    }

    public bool HasProfile => File.Exists(_path);

    public SetupProfile? Load()
    {
        if (!File.Exists(_path)) return null;
        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<SetupProfile>(json, Options);
    }

    public void Save(SetupProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(_path, JsonSerializer.Serialize(profile, Options));
    }
}
