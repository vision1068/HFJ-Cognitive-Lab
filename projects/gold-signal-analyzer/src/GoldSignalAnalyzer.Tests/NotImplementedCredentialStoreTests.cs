using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.Desktop;
using GoldSignalAnalyzer.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE R6 (A07 — auth-bypass-by-default): the deferred <see cref="ICredentialStore"/> must have NO
/// benign no-op Cycle-1 binding. This test proves (a) the DI-resolved binding IS the throwing type
/// (not a silently-swapped null/empty no-op), and (b) EVERY member throws. A regression that made,
/// e.g., GetSecretAsync return null — the exact bypass R6 exists to prevent — turns this suite red.
/// (Closes QA Phase-4 finding: R6 was code-correct but previously untested.)
/// </summary>
public sealed class NotImplementedCredentialStoreTests
{
    [Fact]
    public void R6_DI_resolved_credential_store_is_the_throwing_type_not_a_noop()
    {
        using var scratch = new ScratchDb();
        using var host = GsaHost.CreateHostBuilder(scratch.DbPath, scratch.LogDir).Build();

        var store = host.Services.GetRequiredService<ICredentialStore>();

        Assert.IsType<NotImplementedCredentialStore>(store);
    }

    [Fact]
    public async Task R6_every_member_throws_and_never_returns_a_value()
    {
        ICredentialStore store = new NotImplementedCredentialStore();

        await Assert.ThrowsAsync<NotImplementedException>(() => store.GetSecretAsync("anyKey"));
        await Assert.ThrowsAsync<NotImplementedException>(() => store.SetSecretAsync("anyKey", "anySecret"));
        await Assert.ThrowsAsync<NotImplementedException>(() => store.DeleteSecretAsync("anyKey"));
    }
}
