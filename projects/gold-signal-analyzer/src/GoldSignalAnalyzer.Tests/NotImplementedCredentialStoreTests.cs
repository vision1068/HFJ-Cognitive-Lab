using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.Desktop;
using GoldSignalAnalyzer.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// GATE R6 (A07 — auth-bypass-by-default). The credential port must never be a benign no-op.
/// Cycle 1 upheld this with a throwing binding; Cycle 2 (FR-37) upholds it with a REAL DPAPI store
/// (<see cref="WindowsCredentialStore"/>) resolved by DI — still not a silent null/empty no-op.
/// The retained <see cref="NotImplementedCredentialStore"/> fallback type is unit-tested here to
/// document the "never a silent no-op" invariant it was built to guarantee.
/// </summary>
public sealed class NotImplementedCredentialStoreTests
{
    [Fact]
    public void R6_DI_resolved_credential_store_is_the_real_dpapi_store_not_a_noop()
    {
        using var scratch = new ScratchDb();
        using var host = GsaHost.CreateHostBuilder(scratch.DbPath, scratch.LogDir).Build();

        var store = host.Services.GetRequiredService<ICredentialStore>();

        // Cycle-2: DI resolves the real DPAPI-backed store (not the throwing type, not a no-op).
        Assert.IsType<WindowsCredentialStore>(store);
    }

    [Fact]
    public async Task R6_fallback_type_every_member_throws_and_never_returns_a_value()
    {
        // The retained fallback type still fails loud on every member — proving the invariant it
        // guards (an unbuilt store must never silently bypass secret handling).
        ICredentialStore store = new NotImplementedCredentialStore();

        await Assert.ThrowsAsync<NotImplementedException>(() => store.GetSecretAsync("anyKey"));
        await Assert.ThrowsAsync<NotImplementedException>(() => store.SetSecretAsync("anyKey", "anySecret"));
        await Assert.ThrowsAsync<NotImplementedException>(() => store.DeleteSecretAsync("anyKey"));
    }
}
