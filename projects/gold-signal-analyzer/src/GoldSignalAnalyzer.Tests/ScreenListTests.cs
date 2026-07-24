using GoldSignalAnalyzer.Desktop;
using GoldSignalAnalyzer.Desktop.Presentation;
using GoldSignalAnalyzer.Desktop.Presentation.Screens;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE R2 (arch §7): the 17 registered screen view-models map 1:1 — count AND identity, no missing,
/// no extra — to the literal §30 screen list checked into the repo (<see cref="Section30Screens"/>).
/// </summary>
public sealed class ScreenListTests
{
    [Fact]
    public void R2_section30_list_and_catalog_both_hold_exactly_17_screens()
    {
        Assert.Equal(17, Section30Screens.Titles.Count);
        Assert.Equal(17, ScreenCatalog.ViewModelTypes.Count);
    }

    [Fact]
    public void R2_catalog_maps_1to1_to_section30_by_order_and_title()
    {
        for (var i = 0; i < ScreenCatalog.ViewModelTypes.Count; i++)
        {
            var vm = (PlaceholderScreenViewModel)Activator.CreateInstance(ScreenCatalog.ViewModelTypes[i])!;
            Assert.Equal(Section30Screens.Titles[i], vm.Title);
        }
    }

    [Fact]
    public void R2_no_extra_and_no_missing_screen_view_models_exist_in_the_assembly()
    {
        // Every concrete placeholder screen VM that physically exists in the Desktop assembly...
        var assemblyScreenTitles = typeof(PlaceholderScreenViewModel).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(PlaceholderScreenViewModel).IsAssignableFrom(t))
            .Select(t => ((PlaceholderScreenViewModel)Activator.CreateInstance(t)!).Title)
            .ToHashSet();

        // ...must be exactly the §30 set — an 18th screen class or an unimplemented §30 entry fails here.
        Assert.Equal(Section30Screens.Titles.ToHashSet(), assemblyScreenTitles);
        Assert.Equal(17, assemblyScreenTitles.Count);

        // ...and the catalog it registers from must cover the same set (no catalog drift).
        var catalogTitles = ScreenCatalog.ViewModelTypes
            .Select(t => ((PlaceholderScreenViewModel)Activator.CreateInstance(t)!).Title)
            .ToHashSet();
        Assert.Equal(Section30Screens.Titles.ToHashSet(), catalogTitles);
    }

    [Fact]
    public void R2_every_section30_screen_is_registered_and_resolvable_from_the_real_container()
    {
        using var scratch = new ScratchDb();
        using var host = GsaHost.CreateHostBuilder(scratch.DbPath, scratch.LogDir).Build();

        foreach (var vmType in ScreenCatalog.ViewModelTypes)
        {
            var resolved = host.Services.GetService(vmType);
            Assert.NotNull(resolved);
            Assert.IsType(vmType, resolved);
        }
    }
}
