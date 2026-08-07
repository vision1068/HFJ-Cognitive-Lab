using GoldSignalAnalyzer.Application.Bridge;
using GoldSignalAnalyzer.Presentation.Setup;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// Cycle 5 (FR-28 / AC-28.7 / NFR-SETUP-1/3) — the JSON-file setup profile store.
/// Durability is proven the hard way (Cycle-2 lesson): a SECOND, fresh store instance on
/// the SAME file reads back exact values, including the exact decimal balance — a store
/// test that reuses one instance only proves an in-process cache. The persisted JSON is
/// also proven to carry no secret (D5-3), and the first-run gate (NeedsSetup) is proven
/// to flip exactly once.
/// </summary>
public class JsonFileSetupProfileStoreTests : IDisposable
{
    private readonly List<string> _temps = new();

    private string NewPath()
    {
        var p = Path.Combine(Path.GetTempPath(), $"gsa-setup-{Guid.NewGuid():N}.json");
        _temps.Add(p);
        return p;
    }

    private static SetupProfile Sample() => new()
    {
        DataSource = DataSourceKind.CsvFile,
        CsvPath = @"C:\data\xauusd_h1.csv",
        SymbolRaw = "XAUUSDm",
        NormalizedSymbol = "XAU/USD",
        SymbolConfidence = 0.89,
        ConnectionMode = Mt5ConnectionMode.ConfiguredReadOnly,
        EndpointHost = "127.0.0.1",
        EndpointPort = 8222,
        AccountLogin = 5_123_456,
        ServerName = "MyBroker-Live",
        CredentialKey = "gsa-mt5-readonly", // logical NAME only — not a secret value
        // deliberately many-digit: TEXT/decimal round-trip must be exact, unlike double
        AccountBalance = 12_345.67890123m,
        SavedAtUtc = "2026-07-31T10:00:00.0000000+00:00",
    };

    [Fact] // AC-28.7: NeedsSetup (== !HasProfile) is true until a profile is saved
    public void NeedsSetup_is_true_until_a_profile_is_saved()
    {
        var path = NewPath();
        var store = new JsonFileSetupProfileStore(path);
        Assert.False(store.HasProfile);
        Assert.Null(store.Load());

        store.Save(Sample());
        Assert.True(store.HasProfile);

        // A fresh store on the same file also sees it (a durable, not in-memory, gate).
        Assert.True(new JsonFileSetupProfileStore(path).HasProfile);
    }

    [Fact] // NFR-SETUP-3: durability — a FRESH store instance reads back every field exactly
    public void Profile_survives_store_reopen_with_every_field_intact()
    {
        var path = NewPath();
        var profile = Sample();

        new JsonFileSetupProfileStore(path).Save(profile);

        var reader = new JsonFileSetupProfileStore(path); // second instance, same file
        var got = reader.Load()!;

        Assert.Equal(profile.DataSource, got.DataSource);
        Assert.Equal(profile.CsvPath, got.CsvPath);
        Assert.Equal(profile.SymbolRaw, got.SymbolRaw);
        Assert.Equal(profile.NormalizedSymbol, got.NormalizedSymbol);
        Assert.Equal(profile.SymbolConfidence, got.SymbolConfidence);
        Assert.Equal(profile.ConnectionMode, got.ConnectionMode);
        Assert.Equal(profile.EndpointHost, got.EndpointHost);
        Assert.Equal(profile.EndpointPort, got.EndpointPort);
        Assert.Equal(profile.AccountLogin, got.AccountLogin);
        Assert.Equal(profile.ServerName, got.ServerName);
        Assert.Equal(profile.CredentialKey, got.CredentialKey);
        Assert.Equal(12_345.67890123m, got.AccountBalance); // exact — proves decimal, not double
        Assert.Equal(profile.SavedAtUtc, got.SavedAtUtc);
    }

    [Fact] // NFR-SETUP-1 (behavioural): the persisted JSON carries no secret-shaped token
    public void Persisted_profile_json_contains_no_secret_token()
    {
        var path = NewPath();
        new JsonFileSetupProfileStore(path).Save(Sample());

        var json = File.ReadAllText(path).ToLowerInvariant();
        foreach (var forbidden in new[] { "password", "passwd", "secret", "investor", "token", "apikey", "otp" })
            Assert.DoesNotContain(forbidden, json);
    }

    [Fact] // Save overwrites a prior profile (re-running setup replaces, never appends)
    public void Save_overwrites_a_prior_profile()
    {
        var path = NewPath();
        var store = new JsonFileSetupProfileStore(path);
        store.Save(Sample());
        store.Save(Sample() with { AccountBalance = 999m, SymbolRaw = "GOLD" });

        var got = new JsonFileSetupProfileStore(path).Load()!;
        Assert.Equal(999m, got.AccountBalance);
        Assert.Equal("GOLD", got.SymbolRaw);
    }

    public void Dispose()
    {
        foreach (var p in _temps)
            try { if (File.Exists(p)) File.Delete(p); } catch { /* best-effort temp cleanup */ }
    }
}
