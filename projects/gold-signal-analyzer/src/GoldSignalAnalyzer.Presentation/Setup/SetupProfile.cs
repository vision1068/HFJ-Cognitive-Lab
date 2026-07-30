using GoldSignalAnalyzer.Application.Bridge;

namespace GoldSignalAnalyzer.Presentation.Setup;

/// <summary>
/// The data source the app should analyse (FR-28 / D5-2). Only the two non-live
/// kinds are selectable; the live MT5 kind is surfaced so the user knows it exists
/// but selecting it is a validation failure (C-3, named-approver gate).
/// </summary>
public enum DataSourceKind
{
    /// <summary>Deterministic built-in sample series — never live (INV-4).</summary>
    SampleDemo = 0,

    /// <summary>Historical CSV file — never live (INV-4); requires a path.</summary>
    CsvFile = 1,

    /// <summary>Live MT5 attach — BLOCKED in this build (C-3, D5-2).</summary>
    Mt5Live = 2
}

/// <summary>
/// FR-28 / NFR-SETUP-1: the immutable, first-run configuration the setup wizard
/// captures and persists. By deliberate design this type carries NO password /
/// investor-password / PIN / OTP / token / secret field — the wizard collects no
/// secret at all (D5-3). <see cref="CredentialKey"/> is a logical *name* only (the
/// key under which a future C-3 connect flow would look up a read-only credential
/// via the existing DPAPI seam), never a credential value. The reflection guard test
/// (extending the <c>Mt5ConnectionOptions</c> guard) enforces the "no secret member"
/// property so it can never regress; the persisted JSON is proven secret-free too.
///
/// Plain, serialisable primitives only (init-only properties) so <c>System.Text.Json</c>
/// — in-framework, no new package (D5-4) — round-trips it durably, including the exact
/// <see cref="AccountBalance"/> decimal.
/// </summary>
public sealed record SetupProfile
{
    /// <summary>Chosen data source. Only non-live kinds are ever persisted (D5-2).</summary>
    public DataSourceKind DataSource { get; init; } = DataSourceKind.SampleDemo;

    /// <summary>Historical CSV path when <see cref="DataSource"/> is CsvFile.</summary>
    public string? CsvPath { get; init; }

    /// <summary>Raw broker symbol the user confirmed as gold (e.g. "XAUUSD").</summary>
    public string SymbolRaw { get; init; } = "";

    /// <summary>Normalised symbol value (e.g. "XAU/USD").</summary>
    public string NormalizedSymbol { get; init; } = "";

    /// <summary>Real <c>GoldSymbolResolver</c> match confidence 0..1 (never a %).</summary>
    public double SymbolConfidence { get; init; }

    /// <summary>Connection mode captured for a *later* (C-3) connect — none attempted now.</summary>
    public Mt5ConnectionMode ConnectionMode { get; init; } = Mt5ConnectionMode.AttachExistingSession;

    /// <summary>Loopback-only host (validated via <c>BridgeEndpoint</c>).</summary>
    public string EndpointHost { get; init; } = "127.0.0.1";

    /// <summary>Bridge port 1..65535 (validated via <c>BridgeEndpoint.Loopback</c>).</summary>
    public int EndpointPort { get; init; } = 8080;

    /// <summary>Mode B only: account login number (NOT a secret).</summary>
    public long? AccountLogin { get; init; }

    /// <summary>Mode B only: broker server name (NOT a secret).</summary>
    public string? ServerName { get; init; }

    /// <summary>Logical credential key NAME only — never a credential value (D5-3).</summary>
    public string? CredentialKey { get; init; }

    /// <summary>Account balance used to size the paper risk plan; must be &gt; 0.</summary>
    public decimal AccountBalance { get; init; }

    /// <summary>ISO-8601 UTC timestamp the profile was saved (audit breadcrumb).</summary>
    public string SavedAtUtc { get; init; } = "";
}
