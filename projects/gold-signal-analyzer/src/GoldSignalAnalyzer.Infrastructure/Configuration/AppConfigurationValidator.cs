using GoldSignalAnalyzer.Application.Configuration;
using Microsoft.Extensions.Options;

namespace GoldSignalAnalyzer.Infrastructure.Configuration;

/// <summary>
/// Fail-loud config validation (arch §6, NFR-9, A04). Combined with <c>.ValidateOnStart()</c> the
/// app crashes at host start on invalid config, never silently at request time.
/// </summary>
public sealed class AppConfigurationValidator : IValidateOptions<AppConfiguration>
{
    public ValidateOptionsResult Validate(string? name, AppConfiguration options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.General.DisplayTimeZone))
            errors.Add("General.DisplayTimeZone must be a non-empty value.");
        if (string.IsNullOrWhiteSpace(options.General.StartupScreen))
            errors.Add("General.StartupScreen must be a non-empty value.");

        if (options.Scoring.BuyThreshold is < 0 or > 100)
            errors.Add("Scoring.BuyThreshold must be within 0..100.");
        if (options.Scoring.SellThreshold is < 0 or > 100)
            errors.Add("Scoring.SellThreshold must be within 0..100.");
        if (options.Scoring.WinningMargin is < 0 or > 100)
            errors.Add("Scoring.WinningMargin must be within 0..100.");

        if (options.DataRetention.SignalHistoryDays <= 0)
            errors.Add("DataRetention.SignalHistoryDays must be greater than 0.");
        if (options.DataRetention.JournalRetentionDays <= 0)
            errors.Add("DataRetention.JournalRetentionDays must be greater than 0.");

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
