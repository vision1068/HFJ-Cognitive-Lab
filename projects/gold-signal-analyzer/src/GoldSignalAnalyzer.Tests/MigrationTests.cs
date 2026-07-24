using System.IO;
using GoldSignalAnalyzer.Desktop;
using GoldSignalAnalyzer.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE B1 / FR-4: exercise AddGsaPersistence() end-to-end (host start runs the migrate-on-start
/// hosted service) against a scratch temp db; assert the 3 representative tables + the EF history
/// table exist via sqlite_master. The scratch db lives under the OS temp path, never AppData.
/// </summary>
public sealed class MigrationTests
{
    [Fact]
    public async Task B1_AddGsaPersistence_migrates_scratch_db_and_creates_expected_tables()
    {
        using var scratch = new ScratchDb();

        // Proof the scratch path is NOT the real AppData DB directory (gate B1).
        Assert.NotEqual(
            Path.GetFullPath(GsaHost.DefaultAppDataRoot),
            Path.GetFullPath(Path.GetDirectoryName(scratch.DbPath)!));

        using var host = GsaHost.CreateHostBuilder(scratch.DbPath, scratch.LogDir).Build();
        await host.StartAsync();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GoldSignalDbContext>();
            var tables = await ReadTableNamesAsync(db);

            Assert.Contains("AppSettings", tables);
            Assert.Contains("SignalHistory", tables);
            Assert.Contains("PaperTrade", tables);
            Assert.Contains("__EFMigrationsHistory", tables);
        }

        await host.StopAsync();
        Assert.True(File.Exists(scratch.DbPath), "Scratch db file should have been created by Migrate().");
    }

    private static async Task<List<string>> ReadTableNamesAsync(GoldSignalDbContext db)
    {
        var names = new List<string>();
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));
        return names;
    }
}
