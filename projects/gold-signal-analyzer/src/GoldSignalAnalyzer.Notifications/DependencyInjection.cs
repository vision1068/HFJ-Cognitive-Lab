using Microsoft.Extensions.DependencyInjection;

namespace GoldSignalAnalyzer.Notifications;

public static class DependencyInjection
{
    /// <summary>Cycle-1 no-op — keeps the composition manifest complete (arch §4).</summary>
    public static IServiceCollection AddGsaNotifications(this IServiceCollection services) => services;
}
