using System.IO;
using Microsoft.Data.Sqlite;

namespace GoldSignalAnalyzer.Tests;

/// <summary>Locates the solution's src root from the test's runtime directory.</summary>
internal static class RepoLayout
{
    public static string FindSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "GoldSignalAnalyzer.sln")))
            dir = dir.Parent;

        if (dir is null)
            throw new InvalidOperationException("Could not locate GoldSignalAnalyzer.sln above the test output directory.");

        return dir.FullName;
    }
}

/// <summary>
/// A scratch SQLite DB + log directory under the OS temp path (gate B1). Guarantees no test touches
/// the real per-user AppData DB. Deletes itself (and releases SQLite file handles) on dispose.
/// </summary>
internal sealed class ScratchDb : IDisposable
{
    public string Root { get; }
    public string DbPath { get; }
    public string LogDir { get; }

    public ScratchDb()
    {
        Root = Path.Combine(Path.GetTempPath(), "gsa-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        DbPath = Path.Combine(Root, "scratch.db");
        LogDir = Path.Combine(Root, "logs");
    }

    public void Dispose()
    {
        try { SqliteConnection.ClearAllPools(); } catch { /* best effort */ }
        try { if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true); } catch { /* best effort */ }
    }
}
