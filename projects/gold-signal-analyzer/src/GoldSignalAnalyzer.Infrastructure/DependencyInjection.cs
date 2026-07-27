using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Configuration;
using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.Infrastructure.Configuration;
using GoldSignalAnalyzer.Infrastructure.Logging;
using GoldSignalAnalyzer.Infrastructure.Security;
using GoldSignalAnalyzer.Infrastructure.Time;
using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace GoldSignalAnalyzer.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Bind + fail-loud validate AppConfiguration (arch §6, NFR-9).</summary>
    public static IServiceCollection AddGsaConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AppConfiguration>()
                .Bind(configuration.GetSection("AppConfiguration"))
                .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AppConfiguration>, AppConfigurationValidator>();
        services.AddSingleton<ConfigExportService>();
        return services;
    }

    /// <summary>
    /// Serilog: console + rolling file, with the global sensitive-data masking enricher (arch §5).
    /// The log directory is injectable so tests write to a temp dir (should-fix QA-F7).
    /// </summary>
    public static IServiceCollection AddGsaLogging(this IServiceCollection services, string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        var logFilePath = Path.Combine(logDirectory, "gsa-.log");

        var logger = new LoggerConfiguration()
            .Enrich.With(new SensitiveDataMaskingEnricher())
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 10_000_000)
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(logger, dispose: true);
        });
        return services;
    }

    /// <summary>
    /// UTC clock + the Cycle-2 DPAPI-backed credential store (arch §5, FR-37). This replaces the
    /// Cycle-1 throwing <see cref="NotImplementedCredentialStore"/> binding; the store holds the
    /// tool's own bridge token only and never a broker credential (arch §5.2).
    ///
    /// Declared <c>[SupportedOSPlatform("windows")]</c> because it composes the DPAPI-backed
    /// <see cref="WindowsCredentialStore"/>; callers (the WPF net8.0-windows Desktop host and the
    /// net8.0-windows test project) are already Windows-targeted, so CA1416 does not cascade.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddGsaInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ICredentialStore>(_ => new WindowsCredentialStore());
        return services;
    }
}
