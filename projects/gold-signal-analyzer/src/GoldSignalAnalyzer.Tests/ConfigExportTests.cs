using System.Reflection;
using GoldSignalAnalyzer.Application.Configuration;
using GoldSignalAnalyzer.Application.Security;
using GoldSignalAnalyzer.Infrastructure.Configuration;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE B3 / QA-F8: assert against the SERIALIZED OUTPUT of ConfigExportService that no denylisted
/// key appears, PLUS a reflection scan over the AppConfiguration SOURCE model (recursing nested
/// types) asserting no secret property exists.
/// </summary>
public sealed class ConfigExportTests
{
    [Fact]
    public void B3_exported_json_contains_no_denylisted_key()
    {
        var json = new ConfigExportService().ExportJson(new AppConfiguration());

        foreach (var denied in SecretDenylist.Names)
        {
            Assert.DoesNotContain(
                $"\"{denied}\"",
                json,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void B3_AppConfiguration_source_model_has_no_secret_property()
    {
        var offenders = FindSecretProperties(typeof(AppConfiguration), new HashSet<Type>());
        Assert.True(offenders.Count == 0, $"Secret property/properties found in AppConfiguration: {string.Join(", ", offenders)}");
    }

    private static List<string> FindSecretProperties(Type type, HashSet<Type> visited)
    {
        var offenders = new List<string>();
        if (!visited.Add(type)) return offenders;

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (SecretDenylist.IsSecret(prop.Name))
                offenders.Add($"{type.Name}.{prop.Name}");

            var pt = UnwrapEnumerable(prop.PropertyType);
            if (pt.Namespace?.StartsWith("GoldSignalAnalyzer", StringComparison.Ordinal) == true
                && !pt.IsEnum)
            {
                offenders.AddRange(FindSecretProperties(pt, visited));
            }
        }
        return offenders;
    }

    private static Type UnwrapEnumerable(Type type)
    {
        if (type.IsGenericType && type.GetGenericArguments().Length == 1)
            return type.GetGenericArguments()[0];
        return type;
    }
}
