namespace GoldSignalAnalyzer.Domain;

/// <summary>
/// A raw, broker-specific instrument code exactly as reported by the MT5
/// terminal (e.g. "XAUUSD", "GOLD", "XAUUSDm", "XAUUSD.a"). Never assumed —
/// always discovered from the terminal's symbol list (FR-9).
/// </summary>
public sealed record BrokerSymbol
{
    public string Raw { get; }

    /// <summary>Optional human-readable description the broker attaches.</summary>
    public string? Description { get; }

    public BrokerSymbol(string raw, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Broker symbol cannot be blank.", nameof(raw));
        Raw = raw.Trim();
        Description = description?.Trim();
    }

    public override string ToString() => Raw;
}
