namespace GoldSignalAnalyzer.Domain;

/// <summary>
/// A confirmed mapping from a broker's raw symbol to the canonical
/// normalized symbol (FR-9). Carries how the mapping was decided so the UI
/// can show whether it was auto-detected or chosen by the user.
/// </summary>
public sealed record SymbolMapping
{
    public BrokerSymbol Broker { get; }
    public NormalizedSymbol Normalized { get; }
    public SymbolMappingSource Source { get; }

    /// <summary>Detector confidence 0..1 (1.0 for an exact manual pick).</summary>
    public double Confidence { get; }

    public SymbolMapping(
        BrokerSymbol broker,
        NormalizedSymbol normalized,
        SymbolMappingSource source,
        double confidence)
    {
        Broker = broker ?? throw new ArgumentNullException(nameof(broker));
        Normalized = normalized ?? throw new ArgumentNullException(nameof(normalized));
        if (confidence is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be within 0..1.");
        Source = source;
        Confidence = confidence;
    }
}

public enum SymbolMappingSource
{
    /// <summary>Chosen automatically by the pattern detector.</summary>
    AutoDetected = 0,

    /// <summary>Explicitly selected/overridden by the user (FR-9).</summary>
    ManualSelection = 1
}
