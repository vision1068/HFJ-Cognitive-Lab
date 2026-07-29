using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GoldSignalAnalyzer.Application.Configuration;
using GoldSignalAnalyzer.Domain.Enums;
using GoldSignalAnalyzer.Infrastructure.Logging;

namespace GoldSignalAnalyzer.Desktop.Presentation.Screens;

/// <summary>
/// FR-50 Connection Wizard INPUT + presentation model (Cycle 2, Slice 2). Collects the
/// NON-SECRET inputs needed to configure a read-only MT5 bridge connection.
///
/// <para>By construction this model exposes NO property whose name matches
/// <c>/password|pwd|investor|otp|withdraw/i</c> — a trading / investor / OTP / withdrawal
/// credential is structurally impossible to enter here (AC-50.1 / AC-50.4, RM1/RM3). The account
/// value held here is an operational IDENTIFIER (a login number that identifies the account), NOT
/// a credential, and it is surfaced to the UI / logs only masked to its last 2 characters via
/// <see cref="MaskedAccountIdentifier"/> / <see cref="ToLogSafeString"/> (AC-50.2, gate condition C17).</para>
///
/// <para>This model performs INPUT + presentation only. Validation lives in
/// <c>ConnectionInputValidator</c>; no live-connection logic is present.</para>
/// </summary>
public sealed partial class ConnectionWizardInputModel : ObservableObject
{
    /// <summary>Filesystem path to the MT5 terminal executable. Masked to its file name for logs (C17).</summary>
    [ObservableProperty]
    private string _terminalPath = string.Empty;

    /// <summary>Broker server name (e.g. "Exness-Real"). Masked for logs — it is broker/identity-adjacent (C17).</summary>
    [ObservableProperty]
    private string _serverName = string.Empty;

    /// <summary>
    /// Account IDENTIFIER (a login number used to identify the account) — NOT a credential. It is
    /// surfaced to the UI / logs only masked to its last 2 characters via
    /// <see cref="MaskedAccountIdentifier"/>; the raw value is never logged (AC-50.2).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaskedAccountIdentifier))]
    private string _accountIdentifier = string.Empty;

    /// <summary>Read-only connection mode (spec §5). Defaults to Mode A.</summary>
    [ObservableProperty]
    private ConnectionMode _connectionMode = ConnectionMode.ModeA;

    /// <summary>Gold symbol to analyse (e.g. XAUUSD).</summary>
    [ObservableProperty]
    private string _goldSymbol = "XAUUSD";

    /// <summary>Minimum signal-confidence threshold, a percentage in the inclusive range [0, 100].</summary>
    [ObservableProperty]
    private double _minimumConfidenceThreshold = 50d;

    /// <summary>
    /// Timeframes enabled for analysis. A valid connection requires a non-empty set (AC-50.3).
    /// Defaults to the gold analysis set (M15/H1/H4/D1); the UI may add/remove entries.
    /// </summary>
    public ObservableCollection<Timeframe> EnabledTimeframes { get; } = new()
    {
        Timeframe.M15,
        Timeframe.H1,
        Timeframe.H4,
        Timeframe.D1
    };

    /// <summary>
    /// The account identifier rendered masked to its last 2 characters (AC-50.2). The raw
    /// <see cref="AccountIdentifier"/> value is never equal to this masked projection.
    /// </summary>
    public string MaskedAccountIdentifier => MaskingHelper.MaskAccount(AccountIdentifier);

    /// <summary>
    /// A log-safe, single-line rendering of the wizard inputs (gate condition C17). The terminal
    /// path is reduced to its file name via <see cref="MaskingHelper.MaskPath"/>, the server name
    /// is masked, and the account is masked to last-2. No raw path / server / account value is ever
    /// emitted — this is the ONLY representation of these fields that is safe to log.
    /// </summary>
    public string ToLogSafeString() =>
        "ConnectionWizardInput { " +
        $"TerminalPath = {MaskingHelper.MaskPath(TerminalPath)}, " +
        $"ServerName = {MaskServer(ServerName)}, " +
        $"Account = {MaskedAccountIdentifier}, " +
        $"ConnectionMode = {ConnectionMode}, " +
        $"GoldSymbol = {GoldSymbol}, " +
        $"EnabledTimeframes = [{string.Join(",", EnabledTimeframes)}], " +
        $"MinimumConfidenceThreshold = {MinimumConfidenceThreshold} }}";

    /// <summary>Masks a broker server name to its last 2 characters — it is broker/identity-adjacent (C17).</summary>
    private static string MaskServer(string? server) => MaskingHelper.MaskAccount(server);
}
