using System.IO;
using System.Xml.Linq;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE R1/B3: enforce the inward-only dependency rule by parsing each inner project's .csproj
/// &lt;ProjectReference&gt; elements (NOT reflected assembly refs, which the compiler strips → false PASS).
/// Domain must reference nothing; every inner project's references must be a subset of the allowed set.
/// </summary>
public sealed class ArchitectureTests
{
    // Allowed project-to-project references per arch §2.1 (inner projects only; Desktop is the root).
    private static readonly IReadOnlyDictionary<string, HashSet<string>> Allowed =
        new Dictionary<string, HashSet<string>>
        {
            ["GoldSignalAnalyzer.Domain"] = new(),
            ["GoldSignalAnalyzer.Application"] = new() { "GoldSignalAnalyzer.Domain" },
            ["GoldSignalAnalyzer.Indicators"] = new() { "GoldSignalAnalyzer.Domain" },
            ["GoldSignalAnalyzer.Signals"] = new() { "GoldSignalAnalyzer.Domain", "GoldSignalAnalyzer.Indicators" },
            ["GoldSignalAnalyzer.Backtesting"] = new() { "GoldSignalAnalyzer.Domain", "GoldSignalAnalyzer.Application", "GoldSignalAnalyzer.Indicators", "GoldSignalAnalyzer.Signals" },
            ["GoldSignalAnalyzer.PaperTrading"] = new() { "GoldSignalAnalyzer.Domain", "GoldSignalAnalyzer.Application" },
            ["GoldSignalAnalyzer.MarketData"] = new() { "GoldSignalAnalyzer.Domain", "GoldSignalAnalyzer.Application" },
            ["GoldSignalAnalyzer.MT5Bridge"] = new() { "GoldSignalAnalyzer.Domain", "GoldSignalAnalyzer.Application", "GoldSignalAnalyzer.MarketData" },
            ["GoldSignalAnalyzer.Persistence"] = new() { "GoldSignalAnalyzer.Domain", "GoldSignalAnalyzer.Application" },
            ["GoldSignalAnalyzer.Notifications"] = new() { "GoldSignalAnalyzer.Domain", "GoldSignalAnalyzer.Application" },
            ["GoldSignalAnalyzer.Infrastructure"] = new() { "GoldSignalAnalyzer.Domain", "GoldSignalAnalyzer.Application" },
        };

    public static IEnumerable<object[]> InnerProjects => Allowed.Keys.Select(k => new object[] { k });

    [Fact]
    public void FR1_Domain_references_nothing()
    {
        var refs = ProjectReferencesOf("GoldSignalAnalyzer.Domain");
        Assert.Empty(refs);
    }

    /// <summary>
    /// R1 gap-guard (QA Phase-4): every inner project on disk must be covered by the allow-map, so a
    /// NEW inner project cannot silently escape the inward-only rule until someone remembers to list it.
    /// Desktop (composition root) and the Tests project are the only exempt csproj.
    /// </summary>
    [Fact]
    public void FR1_R1_every_inner_project_on_disk_is_covered_by_the_allow_map()
    {
        var src = RepoLayout.FindSrcRoot();
        var onDisk = Directory
            .EnumerateFiles(src, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(n => n != "GoldSignalAnalyzer.Desktop" && n != "GoldSignalAnalyzer.Tests")
            .ToHashSet();

        var uncovered = onDisk.Where(n => !Allowed.ContainsKey(n)).OrderBy(n => n).ToList();
        Assert.True(
            uncovered.Count == 0,
            $"Inner project(s) not covered by the inward-only allow-map (add them to Allowed): {string.Join(", ", uncovered)}");
    }

    [Theory]
    [MemberData(nameof(InnerProjects))]
    public void FR1_R1_inner_project_references_are_inward_only(string project)
    {
        var actual = ProjectReferencesOf(project);
        var allowed = Allowed[project];

        foreach (var reference in actual)
        {
            Assert.True(
                allowed.Contains(reference),
                $"{project} references {reference}, which violates the inward-only rule (allowed: [{string.Join(", ", allowed)}]).");
        }
    }

    private static HashSet<string> ProjectReferencesOf(string project)
    {
        var src = RepoLayout.FindSrcRoot();
        var csproj = Path.Combine(src, project, project + ".csproj");
        Assert.True(File.Exists(csproj), $"csproj not found: {csproj}");

        var doc = XDocument.Load(csproj);
        var result = new HashSet<string>();
        foreach (var pr in doc.Descendants("ProjectReference"))
        {
            var include = pr.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include)) continue;
            var name = Path.GetFileNameWithoutExtension(include.Replace('\\', Path.DirectorySeparatorChar));
            result.Add(name);
        }
        return result;
    }
}
