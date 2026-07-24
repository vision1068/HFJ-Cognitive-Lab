namespace GoldSignalAnalyzer.Domain.Entities;

/// <summary>
/// Broker contract specification for a symbol (spec §25 risk-plan inputs).
/// Populated only from real broker data in Cycle 2+; never fabricated (NFR-5).
/// </summary>
public sealed record SymbolSpec(
    string Symbol,
    decimal ContractSize,
    decimal TickSize,
    decimal TickValue,
    int Digits,
    decimal LotStep,
    decimal MinLot,
    decimal MaxLot);
