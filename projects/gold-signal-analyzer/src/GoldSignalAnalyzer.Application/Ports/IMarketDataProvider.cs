using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GoldSignalAnalyzer.Domain.Entities;
using GoldSignalAnalyzer.Domain.Enums;
using GoldSignalAnalyzer.Domain.ValueObjects;

namespace GoldSignalAnalyzer.Application.Ports;

/// <summary>
/// Read-only market-data port (arch §8, spec §9). Returns Domain types only and exposes
/// NO order/trade/position/login method — the no-live-execution invariant made structural.
/// </summary>
/// <remarks>
/// §9 BYTE-EXACT (FR-36, Cycle 2 Slice 1, corrected — see CYCLE2-SLICE1-CORRECTION.md).
/// This interface is now a byte-exact transcription of the user-provided literal spec §9,
/// superseding the earlier "faithful reconstruction" (ADR-7 corrected). The read-only invariant is
/// preserved and still enforced by MarketDataPortShapeTests: no order/trade/position/login/modify/
/// close/deal-shaped member. Streaming is part of the read-only contract: <see cref="StreamTicksAsync"/>
/// and <see cref="StreamCandlesAsync"/> are <c>IAsyncEnumerable&lt;T&gt;</c> — the idiomatic .NET
/// PULL-based async stream. A live provider (e.g. MetaTrader5MarketDataProvider, roadmap) implements
/// these by polling the poll-only MT5 source in a loop and <c>yield return</c>-ing each new value; the
/// caller perceives a pull stream, not a manufactured push. This does not violate no-live-execution.
///
/// FR-10 ("test provider only in development/automated-test environments") is NOT an interface member.
/// It is enforced by environment-conditional DI registration (see MarketData/DependencyInjection.cs),
/// replacing the previously-mistaken <c>IsAvailableInProduction</c> interface property.
/// </remarks>
public interface IMarketDataProvider
{
    string ProviderName { get; }

    Task<ProviderConnectionResult> ConnectAsync(
        CancellationToken cancellationToken);

    Task DisconnectAsync(
        CancellationToken cancellationToken);

    Task<AccountSnapshot?> GetAccountSnapshotAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MarketSymbol>> GetAvailableSymbolsAsync(
        CancellationToken cancellationToken);

    Task<SymbolSpecification> GetSymbolSpecificationAsync(
        string symbol,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Candle>> GetHistoricalCandlesAsync(
        string symbol,
        Timeframe timeframe,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    IAsyncEnumerable<MarketTick> StreamTicksAsync(
        string symbol,
        CancellationToken cancellationToken);

    IAsyncEnumerable<Candle> StreamCandlesAsync(
        string symbol,
        Timeframe timeframe,
        CancellationToken cancellationToken);
}
