using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Application.Abstractions;

/// <summary>
/// FR-10: the single abstraction the rest of the system depends on for market
/// data. Concrete implementations: MetaTrader5MarketDataProvider (live seam),
/// CsvHistoricalMarketDataProvider (backtest/replay), TestMarketDataProvider
/// (automated tests only — physically gated out of production builds).
/// </summary>
public interface IMarketDataProvider
{
    /// <summary>Stable identifier for logging/telemetry (never a secret).</summary>
    string Name { get; }

    /// <summary>
    /// FR-11: true ONLY for a provider that returns genuine live broker data.
    /// The Csv and Test providers return false so a caller can refuse to treat
    /// their output as live and can never fabricate a "live" price path.
    /// </summary>
    bool IsLive { get; }

    ConnectionState State { get; }

    Task<ConnectionState> ConnectAsync(CancellationToken ct = default);

    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>Raw broker symbols as reported by the source (FR-9 input).</summary>
    Task<IReadOnlyList<BrokerSymbol>> GetAvailableSymbolsAsync(CancellationToken ct = default);

    /// <summary>Latest tick for a normalized symbol, or null if unavailable.</summary>
    Task<MarketTick?> GetLatestTickAsync(NormalizedSymbol symbol, CancellationToken ct = default);

    Task<IReadOnlyList<Candle>> GetCandlesAsync(
        NormalizedSymbol symbol, TimeFrame timeFrame, int count, CancellationToken ct = default);
}
