namespace GoldSignalAnalyzer.Domain.Entities;

/// <summary>
/// Broker contract specification for a symbol (spec §9 <c>SymbolSpecification</c>; renamed from the
/// Cycle-1 <c>SymbolSpec</c>). Used by the §25 risk-plan inputs. Populated only from real broker
/// data in Cycle 2+; never fabricated (NFR-5).
/// </summary>
public sealed record SymbolSpecification(
    string Symbol,
    decimal ContractSize,
    decimal TickSize,
    decimal TickValue,
    int Digits,
    decimal LotStep,
    decimal MinLot,
    decimal MaxLot);
