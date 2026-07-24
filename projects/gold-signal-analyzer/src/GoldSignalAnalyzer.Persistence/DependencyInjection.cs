using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Configuration;
using GoldSignalAnalyzer.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GoldSignalAnalyzer.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the SQLite DbContext (path from injected <see cref="PersistenceOptions"/>, gate B1),
    /// the repositories, and the migrate-on-start hosted service.
    /// </summary>
    public static IServiceCollection AddGsaPersistence(this IServiceCollection services)
    {
        services.AddDbContext<GoldSignalDbContext>((sp, options) =>
        {
            var dbPath = sp.GetRequiredService<IOptions<PersistenceOptions>>().Value.DbPath;
            if (string.IsNullOrWhiteSpace(dbPath))
                throw new InvalidOperationException("PersistenceOptions.DbPath is not configured (gate B1).");

            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            options.UseSqlite($"Data Source={dbPath}");
        });

        services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
        services.AddScoped<ISignalHistoryRepository, SignalHistoryRepository>();
        services.AddScoped<IPaperTradeRepository, PaperTradeRepository>();

        services.AddHostedService<DatabaseMigrationHostedService>();
        return services;
    }
}
