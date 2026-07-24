using Microsoft.Extensions.DependencyInjection;

namespace GoldSignalAnalyzer.Desktop.Presentation;

public static class DependencyInjection
{
    /// <summary>
    /// Cycle-1 presentation registration (arch §4/§7). Registers <see cref="INavigationService"/>,
    /// the <see cref="ShellViewModel"/>, and every one of the 17 §30 placeholder screen view-models
    /// from <see cref="ScreenCatalog"/>. A screen missing from the catalog therefore fails the
    /// composition and navigation tests (arch §190).
    /// </summary>
    public static IServiceCollection AddGsaPresentation(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ShellViewModel>();

        foreach (var screenViewModelType in ScreenCatalog.ViewModelTypes)
            services.AddTransient(screenViewModelType);

        return services;
    }
}
