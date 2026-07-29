using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoldSignalAnalyzer.Desktop.Presentation.Screens;

namespace GoldSignalAnalyzer.Desktop.Validation;

/// <summary>A single fail-loud validation failure that NAMES the offending field (AC-50.3).</summary>
public sealed record ConnectionInputError(string Field, string Message);

/// <summary>
/// The structured result of validating a <see cref="ConnectionWizardInputModel"/>. Callers inspect
/// <see cref="Errors"/> (each names its field) rather than a bare bool, so every failure is specific.
/// </summary>
public sealed class ConnectionValidationResult
{
    public ConnectionValidationResult(IReadOnlyList<ConnectionInputError> errors) => Errors = errors;

    /// <summary>All validation failures. Empty when the input is fully valid.</summary>
    public IReadOnlyList<ConnectionInputError> Errors { get; }

    /// <summary>True when there are no validation failures.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>True when at least one failure targets <paramref name="field"/>.</summary>
    public bool HasErrorFor(string field) => Errors.Any(e => e.Field == field);
}

/// <summary>
/// FR-50 fail-loud validation for the Connection Wizard inputs (AC-50.3). Runs BEFORE any connect
/// is attempted and collects a specific, field-named error for each invalid input: missing/invalid
/// terminal path, empty server, empty/unknown gold symbol, empty timeframe set, and out-of-range
/// threshold. Failures are collected (not short-circuited) so every offending field is reported.
/// </summary>
public static class ConnectionInputValidator
{
    public const string TerminalPathField = nameof(ConnectionWizardInputModel.TerminalPath);
    public const string ServerNameField = nameof(ConnectionWizardInputModel.ServerName);
    public const string GoldSymbolField = nameof(ConnectionWizardInputModel.GoldSymbol);
    public const string EnabledTimeframesField = nameof(ConnectionWizardInputModel.EnabledTimeframes);
    public const string MinimumConfidenceThresholdField = nameof(ConnectionWizardInputModel.MinimumConfidenceThreshold);

    private const double ThresholdMin = 0d;
    private const double ThresholdMax = 100d;

    /// <summary>Validates the wizard inputs and returns a structured, field-named result (AC-50.3).</summary>
    public static ConnectionValidationResult Validate(ConnectionWizardInputModel model)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));

        var errors = new List<ConnectionInputError>();

        // Terminal path — required, and must be a valid absolute path.
        if (string.IsNullOrWhiteSpace(model.TerminalPath))
        {
            errors.Add(new ConnectionInputError(TerminalPathField, "Terminal path is required."));
        }
        else if (model.TerminalPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0
                 || !Path.IsPathRooted(model.TerminalPath))
        {
            errors.Add(new ConnectionInputError(TerminalPathField, "Terminal path must be a valid absolute path."));
        }

        // Server name — required.
        if (string.IsNullOrWhiteSpace(model.ServerName))
        {
            errors.Add(new ConnectionInputError(ServerNameField, "Server name is required."));
        }

        // Gold symbol — required, and must be a recognized gold (XAU*) symbol.
        if (string.IsNullOrWhiteSpace(model.GoldSymbol))
        {
            errors.Add(new ConnectionInputError(GoldSymbolField, "Gold symbol is required."));
        }
        else if (model.GoldSymbol.IndexOf("XAU", StringComparison.OrdinalIgnoreCase) < 0)
        {
            errors.Add(new ConnectionInputError(
                GoldSymbolField,
                $"'{model.GoldSymbol}' is not a recognized gold symbol (expected an XAU* symbol)."));
        }

        // Timeframes — at least one must be enabled.
        if (model.EnabledTimeframes.Count == 0)
        {
            errors.Add(new ConnectionInputError(EnabledTimeframesField, "At least one timeframe must be enabled."));
        }

        // Threshold — must be within the inclusive range [0, 100].
        if (model.MinimumConfidenceThreshold < ThresholdMin || model.MinimumConfidenceThreshold > ThresholdMax)
        {
            errors.Add(new ConnectionInputError(
                MinimumConfidenceThresholdField,
                $"Minimum confidence threshold must be between {ThresholdMin} and {ThresholdMax}."));
        }

        return new ConnectionValidationResult(errors);
    }
}
