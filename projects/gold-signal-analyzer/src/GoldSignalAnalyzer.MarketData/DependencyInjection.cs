using GoldSignalAnalyzer.Application.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GoldSignalAnalyzer.MarketData;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the market-data port (arch §4).
    ///
    /// The Cycle-1 production-safe <see cref="NullMarketDataProvider"/> is the default binding: it
    /// resolves everywhere (including Production), reports NotConnected, and never fabricates data.
    ///
    /// FR-10 enforcement lives HERE, not on the interface: spec §9 has no <c>IsAvailableInProduction</c>
    /// member (the earlier reconstruction wrongly added one). "The test provider must only be available
    /// in development and automated-test environments" is a composition concern — a test/mock provider
    /// is registered only when <paramref name="environment"/> is NOT Production. The roadmap
    /// <c>TestMarketDataProvider</c> (FR-10) will bind at the marked seam below; until it exists this
    /// method still threads the environment so the gate is a real, single enforcement point.
    /// </summary>
    public static IServiceCollection AddGsaMarketData(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddSingleton<IMarketDataProvider, NullMarketDataProvider>();

        if (!environment.IsProduction())
        {
            // FR-10 seam: services.AddSingleton<IMarketDataProvider, TestMarketDataProvider>();
            // registered here (non-Production only) when the test provider lands. Never in Production.
        }

        return services;
    }
}
