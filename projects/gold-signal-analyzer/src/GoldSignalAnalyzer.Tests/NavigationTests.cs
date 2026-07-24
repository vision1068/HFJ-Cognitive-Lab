using GoldSignalAnalyzer.Desktop;
using GoldSignalAnalyzer.Desktop.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE FR-5 / FR-29 (arch §7): iterate all 17 screen VM types, resolve each from the SAME DI
/// container the app builds (via <see cref="GsaHost"/>), navigate via <see cref="INavigationService"/>,
/// and assert the shell's <c>CurrentViewModel</c> changes to the requested type — every screen exists
/// and is reachable.
/// </summary>
public sealed class NavigationTests
{
    [Fact]
    public void FR5_FR29_navigating_to_each_of_the_17_screens_sets_current_view_model()
    {
        using var scratch = new ScratchDb();
        using var host = GsaHost.CreateHostBuilder(scratch.DbPath, scratch.LogDir).Build();

        var shell = host.Services.GetRequiredService<ShellViewModel>();
        var navigation = host.Services.GetRequiredService<INavigationService>();

        // The shell must never start on a blank ContentControl — ctor lands on the first screen.
        Assert.NotNull(shell.CurrentViewModel);

        foreach (var vmType in ScreenCatalog.ViewModelTypes)
        {
            navigation.NavigateTo(vmType);

            Assert.NotNull(shell.CurrentViewModel);
            Assert.Equal(vmType, shell.CurrentViewModel!.GetType());
            Assert.Same(navigation.CurrentViewModel, shell.CurrentViewModel);
        }
    }

    [Fact]
    public void FR5_shell_exposes_a_nav_item_per_section30_screen()
    {
        using var scratch = new ScratchDb();
        using var host = GsaHost.CreateHostBuilder(scratch.DbPath, scratch.LogDir).Build();

        var shell = host.Services.GetRequiredService<ShellViewModel>();

        Assert.Equal(ScreenCatalog.ViewModelTypes.Count, shell.NavItems.Count);
        for (var i = 0; i < shell.NavItems.Count; i++)
        {
            Assert.Equal(Section30Screens.Titles[i], shell.NavItems[i].Title);
            Assert.Equal(ScreenCatalog.ViewModelTypes[i], shell.NavItems[i].ViewModelType);
        }
    }
}
