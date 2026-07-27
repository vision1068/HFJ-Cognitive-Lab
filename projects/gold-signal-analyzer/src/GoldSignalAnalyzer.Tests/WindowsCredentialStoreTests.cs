using System.IO;
using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.Infrastructure.Security;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// FR-37 / AC-37.4. Proves the DPAPI-backed <see cref="WindowsCredentialStore"/> round-trips a
/// secret (Set→Get→Delete) headless on Windows, that the persisted blob is opaque ciphertext (the
/// raw secret never appears on disk), and that the raw key name never appears as a filename.
/// </summary>
public sealed class WindowsCredentialStoreTests : IDisposable
{
    private readonly string _dir;

    public WindowsCredentialStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "gsa-cred-" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public async Task AC37_4_set_get_delete_round_trips()
    {
        ICredentialStore store = new WindowsCredentialStore(_dir);
        const string key = "gsa:mt5-bridge-token";
        const string secret = "s3cr3t-bridge-token-abc123";

        Assert.Null(await store.GetSecretAsync(key)); // absent before set

        await store.SetSecretAsync(key, secret);
        Assert.Equal(secret, await store.GetSecretAsync(key)); // round-trips

        await store.DeleteSecretAsync(key);
        Assert.Null(await store.GetSecretAsync(key)); // gone after delete
    }

    [Fact]
    public async Task AC37_4_persisted_blob_is_opaque_and_key_name_is_not_a_filename()
    {
        ICredentialStore store = new WindowsCredentialStore(_dir);
        const string key = "gsa:mt5-bridge-token";
        const string secret = "PLAINTEXT-SHOULD-NEVER-APPEAR-ON-DISK";

        await store.SetSecretAsync(key, secret);

        var files = Directory.GetFiles(_dir);
        Assert.Single(files);

        // Raw secret must not be recoverable from the on-disk bytes.
        var bytes = await File.ReadAllBytesAsync(files[0]);
        var asText = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.DoesNotContain(secret, asText);

        // The logical key must not leak into the filename (hashed).
        Assert.DoesNotContain("mt5-bridge-token", Path.GetFileName(files[0]));
    }

    [Fact]
    public async Task delete_is_idempotent_when_absent()
    {
        ICredentialStore store = new WindowsCredentialStore(_dir);
        await store.DeleteSecretAsync("never-set"); // must not throw
    }
}
