using Microsoft.Extensions.DependencyInjection;

namespace GoldSignalAnalyzer.Indicators;

public static class DependencyInjection
{
    /// <summary>Cycle-1 no-op — keeps the composition manifest complete (arch §4).</summary>
    public static IServiceCollection AddGsaIndicators(this IServiceCollection services) => services;
}
