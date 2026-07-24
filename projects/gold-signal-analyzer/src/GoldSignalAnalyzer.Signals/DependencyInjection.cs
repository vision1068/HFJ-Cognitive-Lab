using Microsoft.Extensions.DependencyInjection;

namespace GoldSignalAnalyzer.Signals;

public static class DependencyInjection
{
    /// <summary>Cycle-1 no-op — keeps the composition manifest complete (arch §4).</summary>
    public static IServiceCollection AddGsaSignals(this IServiceCollection services) => services;
}
