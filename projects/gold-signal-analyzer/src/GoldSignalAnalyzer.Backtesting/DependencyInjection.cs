using Microsoft.Extensions.DependencyInjection;

namespace GoldSignalAnalyzer.Backtesting;

public static class DependencyInjection
{
    /// <summary>Cycle-1 no-op — keeps the composition manifest complete (arch §4).</summary>
    public static IServiceCollection AddGsaBacktesting(this IServiceCollection services) => services;
}
