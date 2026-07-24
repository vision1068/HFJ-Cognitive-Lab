using System.IO;
using GoldSignalAnalyzer.Application;
using GoldSignalAnalyzer.Application.Configuration;
using GoldSignalAnalyzer.Backtesting;
using GoldSignalAnalyzer.Desktop.Presentation;
using GoldSignalAnalyzer.Indicators;
using GoldSignalAnalyzer.Infrastructure;
using GoldSignalAnalyzer.MarketData;
using GoldSignalAnalyzer.MT5Bridge;
using GoldSignalAnalyzer.Notifications;
using GoldSignalAnalyzer.PaperTrading;
using GoldSignalAnalyzer.Persistence;
using GoldSignalAnalyzer.Signals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GoldSignalAnalyzer.Desktop;

/// <summary>
/// The single composition manifest (arch §4). Both the WPF app and the test suite build the SAME
/// validated host from here, so a missing/invalid registration fails identically in both places.
///
/// ValidateOnBuild + ValidateScopes are enabled (gate QA-F4). The DB path and log directory are
/// injectable so tests point at a scratch temp path — no test touches the real AppData DB (gate B1).
///
/// Config source (gate B4): configuration is bound from the "AppConfiguration" / "Persistence"
/// sections of the host configuration (env vars + optional non-committed local files). No secret is
/// ever routed to a committed appsettings; Cycle-2 secrets go to .NET User Secrets / ICredentialStore.
/// </summary>
public static class GsaHost
{
    public static string DefaultAppDataRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GoldSignalAnalyzer");

    public static IHostBuilder CreateHostBuilder(string? dbPathOverride = null, string? logDirOverride = null)
    {
        var appDataRoot = DefaultAppDataRoot;
        var defaultDbPath = Path.Combine(appDataRoot, "gsa.db");
        var logDirectory = logDirOverride ?? Path.Combine(appDataRoot, "logs");

        return Host.CreateDefaultBuilder()
            .UseDefaultServiceProvider(options =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            })
            .ConfigureServices((context, services) =>
            {
                services.AddGsaConfiguration(context.Configuration)
                        .AddGsaLogging(logDirectory)
                        .AddGsaInfrastructure()
                        .AddGsaPersistence()
                        .AddGsaMarketData()
                        .AddGsaApplication()
                        .AddGsaIndicators()
                        .AddGsaSignals()
                        .AddGsaBacktesting()
                        .AddGsaPaperTrading()
                        .AddGsaMt5Bridge()
                        .AddGsaNotifications()
                        .AddGsaPresentation();

                // PersistenceOptions.DbPath: bound from config, AppData default, test override wins (gate B1).
                services.AddOptions<PersistenceOptions>()
                        .Bind(context.Configuration.GetSection("Persistence"))
                        .PostConfigure(o =>
                        {
                            if (string.IsNullOrWhiteSpace(o.DbPath))
                                o.DbPath = defaultDbPath;
                        });

                if (!string.IsNullOrWhiteSpace(dbPathOverride))
                    services.PostConfigure<PersistenceOptions>(o => o.DbPath = dbPathOverride);
            });
    }
}
