using Microsoft.Extensions.DependencyInjection;

namespace GoldSignalAnalyzer.MT5Bridge;

public static class DependencyInjection
{
    /// <summary>Cycle-1 no-op — keeps the composition manifest complete (arch §4).</summary>
    public static IServiceCollection AddGsaMt5Bridge(this IServiceCollection services) => services;
}
