using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using GoldSignalAnalyzer.Desktop.Presentation.Screens;
using GoldSignalAnalyzer.Desktop.Validation;
using GoldSignalAnalyzer.Infrastructure.Logging;
using Xunit;

namespace GoldSignalAnalyzer.Tests;

/// <summary>
/// FR-50 Connection Wizard input + validation model tests. One named test per acceptance criterion:
/// AC-50.1/50.4 (no secret-credential property, by construction), AC-50.2 (account masked last-2),
/// AC-50.3 (fail-loud per-field validation), and gate condition C17 (log-safe representation masks
/// terminal path / server / account and never emits a raw value).
/// </summary>
public sealed class ConnectionWizardModelTests
{
    private static ConnectionWizardInputModel ValidModel() => new()
    {
        TerminalPath = @"C:\Program Files\MetaTrader 5\terminal64.exe",
        ServerName = "Exness-Real",
        AccountIdentifier = "1234567",
        GoldSymbol = "XAUUSD",
        MinimumConfidenceThreshold = 50d
    };

    // ---- AC-50.1 / AC-50.4 -------------------------------------------------------------------

    [Fact]
    public void AC50_1_and_50_4_model_exposes_no_password_investor_otp_or_withdraw_property()
    {
        var forbidden = new Regex("password|pwd|investor|otp|withdraw", RegexOptions.IgnoreCase);

        var offenders = typeof(ConnectionWizardInputModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => forbidden.IsMatch(p.Name))
            .Select(p => p.Name)
            .ToArray();

        Assert.Empty(offenders);
    }

    // ---- AC-50.2 -----------------------------------------------------------------------------

    [Fact]
    public void AC50_2_account_identifier_is_rendered_masked_last2_and_raw_is_not_the_masked_view()
    {
        var model = new ConnectionWizardInputModel { AccountIdentifier = "123456789" };

        Assert.Equal(MaskingHelper.MaskAccount("123456789"), model.MaskedAccountIdentifier);
        Assert.Equal("*******89", model.MaskedAccountIdentifier);
        Assert.NotEqual(model.AccountIdentifier, model.MaskedAccountIdentifier);
        Assert.EndsWith("89", model.MaskedAccountIdentifier);
        Assert.DoesNotContain("1234567", model.MaskedAccountIdentifier);
    }

    // ---- AC-50.3 (one case per failure + a fully-valid case) ---------------------------------

    [Fact]
    public void AC50_3_fully_valid_input_yields_no_errors()
    {
        var result = ConnectionInputValidator.Validate(ValidModel());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void AC50_3_missing_terminal_path_yields_terminal_path_error()
    {
        var model = ValidModel();
        model.TerminalPath = "   ";

        var result = ConnectionInputValidator.Validate(model);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor(ConnectionInputValidator.TerminalPathField));
    }

    [Fact]
    public void AC50_3_invalid_terminal_path_yields_terminal_path_error()
    {
        var model = ValidModel();
        model.TerminalPath = "not-an-absolute-path";

        var result = ConnectionInputValidator.Validate(model);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor(ConnectionInputValidator.TerminalPathField));
    }

    [Fact]
    public void AC50_3_empty_server_name_yields_server_name_error()
    {
        var model = ValidModel();
        model.ServerName = "";

        var result = ConnectionInputValidator.Validate(model);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor(ConnectionInputValidator.ServerNameField));
    }

    [Fact]
    public void AC50_3_empty_gold_symbol_yields_gold_symbol_error()
    {
        var model = ValidModel();
        model.GoldSymbol = "";

        var result = ConnectionInputValidator.Validate(model);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor(ConnectionInputValidator.GoldSymbolField));
    }

    [Fact]
    public void AC50_3_unknown_gold_symbol_yields_gold_symbol_error()
    {
        var model = ValidModel();
        model.GoldSymbol = "EURUSD";

        var result = ConnectionInputValidator.Validate(model);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor(ConnectionInputValidator.GoldSymbolField));
    }

    [Fact]
    public void AC50_3_empty_timeframe_set_yields_timeframe_error()
    {
        var model = ValidModel();
        model.EnabledTimeframes.Clear();

        var result = ConnectionInputValidator.Validate(model);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor(ConnectionInputValidator.EnabledTimeframesField));
    }

    [Fact]
    public void AC50_3_out_of_range_threshold_yields_threshold_error()
    {
        var high = ValidModel();
        high.MinimumConfidenceThreshold = 150d;
        Assert.True(ConnectionInputValidator.Validate(high)
            .HasErrorFor(ConnectionInputValidator.MinimumConfidenceThresholdField));

        var low = ValidModel();
        low.MinimumConfidenceThreshold = -5d;
        Assert.True(ConnectionInputValidator.Validate(low)
            .HasErrorFor(ConnectionInputValidator.MinimumConfidenceThresholdField));
    }

    // ---- C17 ---------------------------------------------------------------------------------

    [Fact]
    public void C17_log_safe_representation_masks_path_server_and_account_and_never_emits_raw()
    {
        var model = new ConnectionWizardInputModel
        {
            TerminalPath = @"C:\Secret\Broker\MetaTrader 5\terminal64.exe",
            ServerName = "Exness-Real-Something",
            AccountIdentifier = "998877"
        };

        var log = model.ToLogSafeString();

        // Terminal path passes through MaskingHelper.MaskPath; the raw path is never emitted.
        Assert.Contains(MaskingHelper.MaskPath(model.TerminalPath), log);
        Assert.DoesNotContain(model.TerminalPath, log);
        Assert.DoesNotContain(@"C:\Secret\Broker", log);

        // Server name is masked; the raw value is never emitted.
        Assert.DoesNotContain("Exness-Real-Something", log);

        // Account identifier is masked last-2; the masked view is present and the raw value is not.
        Assert.Contains(model.MaskedAccountIdentifier, log);
        Assert.DoesNotContain("998877", log);
    }
}
