using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Infrastructure.Providers;

/// <summary>
/// FR-10 live seam. This is a STRUCTURAL SEAM ONLY (per the owner's read-only
/// scope): it never attaches to a real MT5 terminal itself — all terminal
/// interaction lives in the out-of-process Python bridge behind
/// <see cref="IMt5BridgeClient"/>. This class only orchestrates the bridge and
/// enforces the safety invariants:
///   * <see cref="IsLive"/> is true, so callers know this path is live-only.
///   * It NEVER fabricates a price. If it is not <see cref="ConnectionState.Connected"/>,
///     data calls throw rather than returning a made-up value (FR-11 / NFR-5).
///   * It refuses to serve data for a symbol with no confirmed broker mapping (FR-9).
/// No composition root wires this to a live bridge in this cycle.
/// </summary>
public sealed class MetaTrader5MarketDataProvider : IMarketDataProvider
{
    private readonly IMt5BridgeClient _bridge;
    private SymbolMapping? _mapping;

    public string Name => "MetaTrader5";
    public bool IsLive => true;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public MetaTrader5MarketDataProvider(IMt5BridgeClient bridge)
        => _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));

    /// <summary>Confirms the broker symbol that maps to a normalized symbol (FR-9).</summary>
    public void UseSymbolMapping(SymbolMapping mapping)
        => _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));

    public async Task<ConnectionState> ConnectAsync(CancellationToken ct = default)
    {
        State = ConnectionState.Connecting;
        var health = await _bridge.CheckHealthAsync(ct).ConfigureAwait(false);
        State = health switch
        {
            { IsHealthy: true, TerminalConnected: true } => ConnectionState.Connected,
            // Bridge is up but the terminal is not attached yet — stay in a
            // reconnecting posture (NFR-4), never Connected, never fabricate.
            { IsHealthy: true, TerminalConnected: false } => ConnectionState.Reconnecting,
            _ => ConnectionState.Faulted
        };
        return State;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        State = ConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<BrokerSymbol>> GetAvailableSymbolsAsync(CancellationToken ct = default)
    {
        EnsureConnected();
        return await _bridge.ListSymbolsAsync(ct).ConfigureAwait(false);
    }

    public async Task<MarketTick?> GetLatestTickAsync(NormalizedSymbol symbol, CancellationToken ct = default)
    {
        var broker = RequireBrokerSymbolFor(symbol);
        return await _bridge.GetTickAsync(broker, symbol, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Candle>> GetCandlesAsync(NormalizedSymbol symbol, TimeFrame timeFrame, int count, CancellationToken ct = default)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        var broker = RequireBrokerSymbolFor(symbol);
        return await _bridge.GetCandlesAsync(broker, symbol, timeFrame, count, ct).ConfigureAwait(false);
    }

    private void EnsureConnected()
    {
        if (State != ConnectionState.Connected)
            throw new InvalidOperationException(
                $"MT5 provider is '{State}', not Connected — refusing to serve market data (FR-11: never fabricate live data).");
    }

    private string RequireBrokerSymbolFor(NormalizedSymbol symbol)
    {
        EnsureConnected();
        if (_mapping is null || !_mapping.Normalized.Equals(symbol))
            throw new InvalidOperationException(
                $"No confirmed broker symbol mapped for '{symbol}' (FR-9). Detect or select one before requesting data.");
        return _mapping.Broker.Raw;
    }
}
