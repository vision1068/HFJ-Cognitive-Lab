namespace GoldSignalAnalyzer.Domain;

/// <summary>
/// A broker-independent, canonical instrument identifier (FR-9). The whole
/// system reasons in normalized symbols; only the provider edge deals in
/// raw broker symbols. Gold normalizes to <see cref="Gold"/> = "XAU/USD".
/// </summary>
public sealed record NormalizedSymbol
{
    /// <summary>Canonical normalized gold symbol required by FR-9.</summary>
    public static readonly NormalizedSymbol Gold = new("XAU/USD");

    public string Value { get; }

    public NormalizedSymbol(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Normalized symbol cannot be blank.", nameof(value));
        Value = value.Trim();
    }

    public override string ToString() => Value;
}
