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
    // Cycle 9 (IC-1): optional per-timeframe candle sets, so the LTF pull and the HTF pull
    // (which requests a DIFFERENT, stepped-up timeframe) can return DIFFERENT series — the
    // only way to construct the "bullish LTF proposal + bearish HTF → Neutral" regression.
    private readonly Dictionary<TimeFrame, List<Candle>> _candlesByTimeFrame = new();
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

    /// <summary>Cycle 9 (IC-1): supply candles for a SPECIFIC timeframe. A
    /// <see cref="GetCandlesAsync"/> for that timeframe returns these; any other timeframe
    /// falls back to the default <see cref="WithCandles(IEnumerable{Candle})"/> set.</summary>
    public TestMarketDataProvider WithCandles(TimeFrame timeFrame, IEnumerable<Candle> candles)
    {
        if (!_candlesByTimeFrame.TryGetValue(timeFrame, out var list))
            _candlesByTimeFrame[timeFrame] = list = new List<Candle>();
        list.AddRange(candles);
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

    private readonly List<TimeFrame> _requestedTimeFrames = new();

    /// <summary>Cycle 9 (IC-1/IC-3): the ORDERED sequence of timeframes requested via
    /// <see cref="GetCandlesAsync"/>, so a test can assert a single refresh pulled exactly
    /// <c>[snapshotTF, StepUp(snapshotTF)]</c> and that the HTF pull used the SNAPSHOTTED
    /// timeframe (not a value mutated mid-poll).</summary>
    public IReadOnlyList<TimeFrame> RequestedTimeFrames => _requestedTimeFrames;

    /// <summary>Cycle 9: an optional gate a test can await/complete to hold a
    /// <see cref="GetCandlesAsync"/> call in flight (proves the start-of-refresh snapshot).</summary>
    public TaskCompletionSource? Gate { get; set; }

    private TimeFrame? _throwOnTimeFrame;

    /// <summary>Cycle 9 (IC-2): make <see cref="GetCandlesAsync"/> THROW for a specific
    /// timeframe, so a test can prove the coordinator's HTF pull fail-safe (the scoped catch
    /// degrades to a clean guard-Neutral and no exception escapes the refresh).</summary>
    public TestMarketDataProvider ThrowOnTimeFrame(TimeFrame timeFrame)
    {
        _throwOnTimeFrame = timeFrame;
        return this;
    }

    public async Task<IReadOnlyList<Candle>> GetCandlesAsync(NormalizedSymbol symbol, TimeFrame timeFrame, int count, CancellationToken ct = default)
    {
        LastRequestedTimeFrame = timeFrame;
        _requestedTimeFrames.Add(timeFrame);
        if (Gate is not null) await Gate.Task.ConfigureAwait(false);
        if (_throwOnTimeFrame == timeFrame)
            throw new InvalidOperationException($"Simulated feed failure for {timeFrame}.");
        var source = _candlesByTimeFrame.TryGetValue(timeFrame, out var tfCandles) ? tfCandles : _candles;
        return source.TakeLast(count).ToList();
    }
}
