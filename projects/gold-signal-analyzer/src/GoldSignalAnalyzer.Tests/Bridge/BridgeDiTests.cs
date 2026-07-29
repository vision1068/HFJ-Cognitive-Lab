using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.MarketData;
using GoldSignalAnalyzer.MT5Bridge;
using GoldSignalAnalyzer.MT5Bridge.Process;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GoldSignalAnalyzer.Tests.Bridge;

/// <summary>
/// DI posture (ADR-12 / FR-49 / C9 / C12): going live is an EXPLICIT keyed opt-in, never an ambient
/// swap. In the default (Production, opt-in OFF) composition the keyless <see cref="IMarketDataProvider"/>
/// stays <see cref="NullMarketDataProvider"/> and the live-capable services (keyed provider + the
/// <see cref="IBridgeProcess"/> spawner) are UNRESOLVABLE. Only with the explicit opt-in do they
/// register, and then the keyed provider forwards to the SAME concrete singleton (one child process).
/// </summary>
public sealed class BridgeDiTests
{
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "GoldSignalAnalyzer";
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static ServiceProvider BuildDefault()
    {
        var services = new ServiceCollection();
        // Same as the Production composition root: keyless default provider + AddGsaMt5Bridge() with no
        // opt-in argument.
        services.AddGsaMarketData(new FakeHostEnvironment { EnvironmentName = Environments.Production });
        services.AddGsaMt5Bridge(); // enableLiveBridge defaults to false
        return services.BuildServiceProvider();
    }

    [Fact] // AC-49.1: keyless default is NullMarketDataProvider even in Production
    public void Default_keyless_provider_is_Null_in_Production()
    {
        using var sp = BuildDefault();
        Assert.IsType<NullMarketDataProvider>(sp.GetRequiredService<IMarketDataProvider>());
    }

    [Fact] // C9: with the opt-in OFF, the keyed live provider is UNRESOLVABLE
    public void Default_has_no_keyed_bridge_provider()
    {
        using var sp = BuildDefault();
        Assert.Null(sp.GetKeyedService<IMarketDataProvider>("mt5-bridge"));
    }

    [Fact] // C9: with the opt-in OFF, the child-process SPAWNER is UNRESOLVABLE (nothing can go live)
    public void Default_has_no_bridge_process_spawner()
    {
        using var sp = BuildDefault();
        Assert.Null(sp.GetService<IBridgeProcess>());
        Assert.Null(sp.GetService<Mt5BridgeClient>());
    }

    // -- explicit opt-in ---------------------------------------------------------------------------

    private static ServiceProvider BuildOptedIn()
    {
        var services = new ServiceCollection();
        // Deps the client needs that come from other layers in the real app.
        services.AddSingleton<ICredentialStore>(new FakeCredentialStore());
        services.AddSingleton<IClock>(new FixedClock(DateTime.UtcNow));
        services.AddGsaMt5Bridge(enableLiveBridge: true);
        return services.BuildServiceProvider();
    }

    [Fact] // AC-49.2: the keyed provider resolves to Mt5BridgeClient
    public void OptIn_keyed_provider_is_Mt5BridgeClient()
    {
        using var sp = BuildOptedIn();
        var keyed = sp.GetKeyedService<IMarketDataProvider>("mt5-bridge");
        Assert.IsType<Mt5BridgeClient>(keyed);
    }

    [Fact] // C12: keyed forwards to the SAME concrete singleton — one client, one child process
    public void OptIn_keyed_and_concrete_resolve_the_same_singleton()
    {
        using var sp = BuildOptedIn();
        var keyed = sp.GetKeyedService<IMarketDataProvider>("mt5-bridge");
        var concrete = sp.GetRequiredService<Mt5BridgeClient>();
        Assert.Same(concrete, keyed);
    }

    [Fact] // AC-49.3: even opted-in, the bridge is NEVER bound as the keyless default
    public void OptIn_does_not_bind_the_keyless_default_to_the_bridge()
    {
        using var sp = BuildOptedIn();
        // No AddGsaMarketData here, so the keyless default is simply absent — proving AddGsaMt5Bridge
        // did not register itself as the keyless IMarketDataProvider.
        Assert.Null(sp.GetService<IMarketDataProvider>());
    }
}
