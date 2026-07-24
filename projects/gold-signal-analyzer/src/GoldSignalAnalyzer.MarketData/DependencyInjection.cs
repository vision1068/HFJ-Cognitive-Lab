using GoldSignalAnalyzer.Application.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace GoldSignalAnalyzer.MarketData;

public static class DependencyInjection
{
    /// <summary>Resolves the market-data port to the safe Cycle-1 NullMarketDataProvider (arch §4).</summary>
    public static IServiceCollection AddGsaMarketData(this IServiceCollection services)
    {
        services.AddSingleton<IMarketDataProvider, NullMarketDataProvider>();
        return services;
    }
}
