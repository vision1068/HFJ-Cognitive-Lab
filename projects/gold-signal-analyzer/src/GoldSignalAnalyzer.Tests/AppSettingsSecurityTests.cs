using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Security;
using GoldSignalAnalyzer.Desktop;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE B3: writing a denylisted key to the AppSettings key/value table is rejected at write time;
/// a non-secret key round-trips normally.
/// </summary>
public sealed class AppSettingsSecurityTests
{
    [Fact]
    public async Task B3_writing_denylisted_key_throws_and_non_secret_key_roundtrips()
    {
        using var scratch = new ScratchDb();
        using var host = GsaHost.CreateHostBuilder(scratch.DbPath, scratch.LogDir).Build();
        await host.StartAsync();

        using var scope = host.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAppSettingsRepository>();

        await Assert.ThrowsAsync<SecretPersistenceException>(() => repo.SetAsync("password", "hunter2"));
        await Assert.ThrowsAsync<SecretPersistenceException>(() => repo.SetAsync("Token", "abc"));

        await repo.SetAsync("Theme", "Dark");
        Assert.Equal("Dark", await repo.GetAsync("Theme"));

        await host.StopAsync();
    }
}
