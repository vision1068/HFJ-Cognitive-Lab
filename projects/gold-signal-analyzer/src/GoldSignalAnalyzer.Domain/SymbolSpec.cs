namespace GoldSignalAnalyzer.Domain;

/// <summary>
/// FR-25: real broker contract specification for a symbol, used for risk /
/// position-size math. For spot gold a typical spec is ContractSize = 100 oz
/// per 1.00 lot, MinLot 0.01, LotStep 0.01, Digits 2. These come from the real
/// terminal's symbol info — never hardcoded per broker (FR-9 spirit).
/// </summary>
public sealed record SymbolSpec
{
    public NormalizedSymbol Symbol { get; }
    public int Digits { get; }
    /// <summary>Units of the base asset per 1.00 lot (e.g. 100 oz for XAUUSD).</summary>
    public decimal ContractSize { get; }
    public decimal MinLot { get; }
    public decimal LotStep { get; }
    public decimal MaxLot { get; }

    public SymbolSpec(NormalizedSymbol symbol, int digits, decimal contractSize,
                      decimal minLot, decimal lotStep, decimal maxLot)
    {
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        if (digits < 0) throw new ArgumentOutOfRangeException(nameof(digits));
        if (contractSize <= 0) throw new ArgumentOutOfRangeException(nameof(contractSize));
        if (minLot <= 0) throw new ArgumentOutOfRangeException(nameof(minLot));
        if (lotStep <= 0) throw new ArgumentOutOfRangeException(nameof(lotStep));
        if (maxLot < minLot) throw new ArgumentException("MaxLot cannot be below MinLot.");
        Digits = digits; ContractSize = contractSize;
        MinLot = minLot; LotStep = lotStep; MaxLot = maxLot;
    }

    /// <summary>A conventional spot-gold spec (100 oz/lot). Values still come
    /// from a real terminal in production; this is a sane default for tests.</summary>
    public static SymbolSpec Gold() =>
        new(NormalizedSymbol.Gold, digits: 2, contractSize: 100m, minLot: 0.01m, lotStep: 0.01m, maxLot: 100m);
}
