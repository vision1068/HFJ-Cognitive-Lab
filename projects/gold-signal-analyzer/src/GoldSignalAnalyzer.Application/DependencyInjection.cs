using Microsoft.Extensions.DependencyInjection;

namespace GoldSignalAnalyzer.Application;

public static class DependencyInjection
{
    /// <summary>Cycle-1: no use-case services yet. Placeholder keeps the composition manifest complete.</summary>
    public static IServiceCollection AddGsaApplication(this IServiceCollection services) => services;
}
