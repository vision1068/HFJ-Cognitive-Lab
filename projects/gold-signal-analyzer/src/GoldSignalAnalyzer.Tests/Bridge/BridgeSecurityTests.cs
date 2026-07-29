using GoldSignalAnalyzer.Application.Exceptions;
using GoldSignalAnalyzer.MT5Bridge;
using GoldSignalAnalyzer.MT5Bridge.Protocol;
using Xunit;

namespace GoldSignalAnalyzer.Tests.Bridge;

/// <summary>
/// Bridge-token posture (C10.1 / NFR-1). When no token exists the client MINTS a fresh one from a
/// CSPRNG (32 bytes → base64), persists it via <see cref="ICredentialStore"/>, and binds it to the
/// transport as the Bearer credential. When a token already exists it is REUSED (no re-mint). The
/// token travels only as the Authorization header value (asserted elsewhere: no DTO field, no logging
/// handler) — never an arg/env/config.
/// </summary>
public sealed class BridgeSecurityTests
{
    private static Mt5BridgeClient NewClient(FakeBridgeTransport transport, FakeCredentialStore creds)
        => new(new FakeBridgeProcess(), transport, creds, new FixedClock(DateTime.UtcNow));

    [Fact] // C10.1: absent → generate a 32-byte CSPRNG token, store it, and bind it
    public async Task ConnectAsync_mints_and_stores_a_32_byte_token_when_absent()
    {
        var creds = new FakeCredentialStore(); // empty
        var transport = new FakeBridgeTransport();
        await using var client = NewClient(transport, creds);

        await client.ConnectAsync(CancellationToken.None);

        Assert.Contains(Mt5BridgeClient.TokenKey, creds.SetKeys); // persisted
        var stored = creds.Peek(Mt5BridgeClient.TokenKey);
        Assert.False(string.IsNullOrEmpty(stored));
        Assert.Equal(32, Convert.FromBase64String(stored!).Length); // 32 bytes of CSPRNG output
        Assert.Equal(stored, transport.BoundToken);                 // the bound Bearer token is the minted one
    }

    [Fact] // C10.1: present → reuse the existing token, never re-mint
    public async Task ConnectAsync_reuses_an_existing_token()
    {
        const string existing = "an-existing-bridge-token-value";
        var creds = new FakeCredentialStore((Mt5BridgeClient.TokenKey, existing));
        var transport = new FakeBridgeTransport();
        await using var client = NewClient(transport, creds);

        await client.ConnectAsync(CancellationToken.None);

        Assert.Empty(creds.SetKeys);                    // no re-mint / no overwrite
        Assert.Equal(existing, transport.BoundToken);   // the existing token is bound
    }

    [Fact] // two independent mints differ (a real CSPRNG, not a fixed/predictable value)
    public async Task Two_fresh_tokens_are_distinct()
    {
        var t1 = new FakeBridgeTransport();
        var t2 = new FakeBridgeTransport();
        await using (var c1 = NewClient(t1, new FakeCredentialStore()))
            await c1.ConnectAsync(CancellationToken.None);
        await using (var c2 = NewClient(t2, new FakeCredentialStore()))
            await c2.ConnectAsync(CancellationToken.None);

        Assert.NotEqual(t1.BoundToken, t2.BoundToken);
    }

    [Fact] // AC-47.3: a mapped-error exception NEVER embeds the bearer token value
    public async Task Error_exceptions_never_contain_the_token_value()
    {
        const string token = "SECRET-BEARER-abc123-do-not-leak";
        var creds = new FakeCredentialStore((Mt5BridgeClient.TokenKey, token));
        var transport = new FakeBridgeTransport
        {
            // Any live read fails this slice (no terminal attached) — the client maps it to a typed
            // exception; that exception must not carry the token in its message/stack.
            Responder = (_, _) => BridgeResponses.Error(BridgeErrorCodes.HandlerError, "AttributeError"),
        };
        await using var client = NewClient(transport, creds);
        await client.ConnectAsync(CancellationToken.None);

        var ex = await Assert.ThrowsAsync<NotConnectedException>(
            () => client.GetAvailableSymbolsAsync(CancellationToken.None));

        Assert.DoesNotContain(token, ex.ToString());
    }
}
