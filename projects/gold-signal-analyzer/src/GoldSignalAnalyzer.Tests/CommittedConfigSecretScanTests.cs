using System.IO;
using System.Text.RegularExpressions;
using GoldSignalAnalyzer.Application.Security;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE B4: recurring build-gate. Grep every committed appsettings*.json / *.config under src for a
/// denylisted secret key. A later hand-added secret in a committed config file fails this test.
/// </summary>
public sealed class CommittedConfigSecretScanTests
{
    [Fact]
    public void B4_no_committed_config_file_contains_a_secret_key()
    {
        var src = RepoLayout.FindSrcRoot();

        var configFiles = Directory
            .EnumerateFiles(src, "*", SearchOption.AllDirectories)
            .Where(IsScannableConfigFile)
            .Where(f => !IsBuildArtifact(f))
            .ToList();

        foreach (var file in configFiles)
        {
            var text = File.ReadAllText(file);
            foreach (var key in SecretDenylist.Names)
            {
                var pattern = $"\"{Regex.Escape(key)}\"";
                Assert.False(
                    Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase),
                    $"Secret key '{key}' found in committed config file: {file}");
            }
        }
    }

    private static bool IsScannableConfigFile(string path)
    {
        var name = Path.GetFileName(path);
        var isAppsettings = name.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)
                            && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
        var isConfig = name.EndsWith(".config", StringComparison.OrdinalIgnoreCase);
        return isAppsettings || isConfig;
    }

    private static bool IsBuildArtifact(string path)
    {
        var sep = Path.DirectorySeparatorChar;
        return path.Contains($"{sep}bin{sep}", StringComparison.OrdinalIgnoreCase)
               || path.Contains($"{sep}obj{sep}", StringComparison.OrdinalIgnoreCase);
    }
}
