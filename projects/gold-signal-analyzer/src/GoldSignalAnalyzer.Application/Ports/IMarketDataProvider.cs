using GoldSignalAnalyzer.Domain.Entities;
using GoldSignalAnalyzer.Domain.Enums;
using GoldSignalAnalyzer.Domain.ValueObjects;

namespace GoldSignalAnalyzer.Application.Ports;

/// <summary>
/// Read-only market-data port (arch §8, spec §9). Returns Domain types only and exposes
/// NO order/trade method — the no-live-execution invariant made structural.
/// Surface is reduced to the minimum the Cycle-1 NullMarketDataProvider + DI need (gate QA-F10).
/// </summary>
// [NEEDS CLARIFICATION] verbatim spec §9 shape reconciled in Cycle 2
public interface IMarketDataProvider
{
    string Name { get; }

    /// <summary>False for the test provider so it is gated out of production builds (FR-10).</summary>
    bool IsAvailableInProduction { get; }

    Task<ConnectionStatus> ConnectAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SymbolInfo>> GetAvailableSymbolsAsync(CancellationToken ct = default);

    /// <summary>Scalar: a real contract spec or throw. A default SymbolSpec is fabricated data (NFR-5).</summary>
    Task<SymbolSpec> GetSymbolSpecAsync(string symbol, CancellationToken ct = default);

    Task<IReadOnlyList<Candle>> GetCandlesAsync(string symbol, Timeframe timeframe, int count, CancellationToken ct = default);

    /// <summary>Scalar: a real tick or throw. A default Tick is fabricated data (NFR-5).</summary>
    Task<Tick> GetLatestTickAsync(string symbol, CancellationToken ct = default);

    Task DisconnectAsync(CancellationToken ct = default);
}
