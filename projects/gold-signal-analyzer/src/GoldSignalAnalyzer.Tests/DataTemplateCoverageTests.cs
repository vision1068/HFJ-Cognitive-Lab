using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Xml.Linq;
using GoldSignalAnalyzer.Desktop.Presentation;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE R3 (arch §7): every one of the 17 screen view-models has a corresponding View DataTemplate.
/// A headless VM-only navigation must NOT leave this green if a View/DataTemplate is missing — so
/// both the checked-in template list is reconciled to all 17 AND each View is really instantiated.
/// </summary>
public sealed class DataTemplateCoverageTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static string TemplatesFile()
    {
        var src = RepoLayout.FindSrcRoot();
        return Path.Combine(src, "GoldSignalAnalyzer.Desktop", "Views", "ScreenTemplates.xaml");
    }

    [Fact]
    public void R3_template_file_declares_exactly_one_DataTemplate_per_screen_VM()
    {
        var doc = XDocument.Load(TemplatesFile());
        var templates = doc.Descendants(Presentation + "DataTemplate").ToList();

        var declaredVmNames = templates
            .Select(t => (string?)t.Attribute("DataType"))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => ExtractLocalTypeName(v!))
            .ToHashSet();

        var expected = ScreenCatalog.ViewModelTypes.Select(t => t.Name).ToHashSet();

        Assert.Equal(17, templates.Count);
        Assert.Equal(expected, declaredVmNames); // count + identity: no missing, no extra
    }

    [Fact]
    public void R3_every_screen_VM_template_instantiates_its_correct_View()
    {
        StaExecutor.Run(() =>
        {
            using var stream = File.OpenRead(TemplatesFile());
            var dictionary = (ResourceDictionary)XamlReader.Load(stream);

            var vmToViewType = new Dictionary<Type, Type>();
            foreach (var value in dictionary.Values)
            {
                if (value is DataTemplate template && template.DataType is Type vmType)
                {
                    // LoadContent actually builds the View visual — a missing/typo'd View throws here.
                    var view = template.LoadContent();
                    vmToViewType[vmType] = view.GetType();
                }
            }

            foreach (var vmType in ScreenCatalog.ViewModelTypes)
            {
                Assert.True(vmToViewType.ContainsKey(vmType), $"No View DataTemplate for {vmType.Name}.");
                Assert.Equal(vmType.Name.Replace("ViewModel", "View"), vmToViewType[vmType].Name);
            }

            Assert.Equal(ScreenCatalog.ViewModelTypes.Count, vmToViewType.Count); // no extra templates
        });
    }

    // "{x:Type vm:DashboardHomeViewModel}" -> "DashboardHomeViewModel"
    private static string ExtractLocalTypeName(string dataType)
    {
        var trimmed = dataType.Trim().TrimStart('{').TrimEnd('}').Trim();
        var lastSpace = trimmed.LastIndexOf(' ');
        var token = lastSpace >= 0 ? trimmed[(lastSpace + 1)..] : trimmed;
        var colon = token.LastIndexOf(':');
        return colon >= 0 ? token[(colon + 1)..] : token;
    }
}
