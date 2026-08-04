using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Testing;

/// <summary>
/// FR-10: an <see cref="IMarketDataProvider"/> for AUTOMATED TESTS ONLY.
///
/// This type is deliberately in a SEPARATE assembly
/// (GoldSignalAnalyzer.Testing) that the production Infrastructure/composition
/// projects do NOT reference. That is the gate required by FR-10/NFR-10: the
/// test provider physically cannot be resolved from a production build, so no
/// runtime flag is needed to keep it out. A guard test asserts this property.
///
/// <see cref="IsLive"/> is false — its scripted data must never be read as a
/// live quote (FR-11).
/// </summary>
public sealed class TestMarketDataProvider : IMarketDataProvider
{
    private readonly List<BrokerSymbol> _symbols = new();
    private readonly List<Candle> _candles = new();
    private MarketTick? _tick;

    public string Name => "Test";
    public bool IsLive => false;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public TestMarketDataProvider WithSymbols(params BrokerSymbol[] symbols)
    {
        _symbols.AddRange(symbols);
        return this;
    }

    public TestMarketDataProvider WithLatestTick(MarketTick tick)
    {
        _tick = tick;
        return this;
    }

    public TestMarketDataProvider WithCandles(IEnumerable<Candle> candles)
    {
        _candles.AddRange(candles);
        return this;
    }

    public Task<ConnectionState> ConnectAsync(CancellationToken ct = default)
    {
        State = ConnectionState.Connected;
        return Task.FromResult(State);
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        State = ConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BrokerSymbol>> GetAvailableSymbolsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<BrokerSymbol>>(_symbols.ToList());

    public Task<MarketTick?> GetLatestTickAsync(NormalizedSymbol symbol, CancellationToken ct = default)
        => Task.FromResult(_tick);

    /// <summary>The timeframe of the most recent <see cref="GetCandlesAsync"/> call, so a
    /// test can assert the coordinator re-pulled at a newly-selected timeframe (Cycle 8 FR-40).</summary>
    public TimeFrame? LastRequestedTimeFrame { get; private set; }

    public Task<IReadOnlyList<Candle>> GetCandlesAsync(NormalizedSymbol symbol, TimeFrame timeFrame, int count, CancellationToken ct = default)
    {
        LastRequestedTimeFrame = timeFrame;
        return Task.FromResult<IReadOnlyList<Candle>>(_candles.TakeLast(count).ToList());
    }
}
