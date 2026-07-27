namespace GoldSignalAnalyzer.Domain.ValueObjects;

/// <summary>
/// A read-only point-in-time snapshot of the connected account (spec §9
/// <c>GetAccountSnapshotAsync</c>). Populated only from real broker data in Cycle 2+; never
/// fabricated (NFR-5). Carries no order/trade capability — purely observational balance figures.
/// The nullable return of <c>GetAccountSnapshotAsync</c> honestly represents "no account / not
/// connected" without fabricating a zero-valued account.
/// </summary>
public sealed record AccountSnapshot(
    string AccountId,
    string Currency,
    decimal Balance,
    decimal Equity,
    decimal FreeMargin);
