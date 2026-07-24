using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.Desktop;
using GoldSignalAnalyzer.Desktop.Presentation;
using GoldSignalAnalyzer.MarketData;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE FR-2 / QA-F4: build the SAME validated provider the app uses (ValidateOnBuild +
/// ValidateScopes) against a SCRATCH db path, and resolve ShellViewModel + the market-data port.
/// GATE arch §190: the composition test also resolves each of the 17 placeholder screen VMs, so a
/// missing screen registration fails the test.
/// </summary>
public sealed class DiCompositionTests
{
    [Fact]
    public void FR2_host_builds_validated_and_resolves_ShellViewModel_and_market_data_port()
    {
        using var scratch = new ScratchDb();

        // Build() triggers ValidateOnBuild — a missing/invalid registration throws here.
        using var host = GsaHost.CreateHostBuilder(scratch.DbPath, scratch.LogDir).Build();

        var shell = host.Services.GetRequiredService<ShellViewModel>();
        Assert.NotNull(shell);

        var navigation = host.Services.GetRequiredService<INavigationService>();
        Assert.NotNull(navigation);

        var provider = host.Services.GetRequiredService<IMarketDataProvider>();
        Assert.IsType<NullMarketDataProvider>(provider);
    }

    [Fact]
    public void Arch190_each_of_the_17_screen_view_models_resolves_from_the_container()
    {
        using var scratch = new ScratchDb();
        using var host = GsaHost.CreateHostBuilder(scratch.DbPath, scratch.LogDir).Build();

        foreach (var vmType in ScreenCatalog.ViewModelTypes)
        {
            var resolved = host.Services.GetRequiredService(vmType);
            Assert.IsType(vmType, resolved);
        }
    }
}
