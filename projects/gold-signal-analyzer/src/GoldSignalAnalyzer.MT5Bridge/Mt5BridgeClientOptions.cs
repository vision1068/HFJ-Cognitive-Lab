namespace GoldSignalAnalyzer.MT5Bridge;

/// <summary>
/// Runtime knobs for <see cref="Mt5BridgeClient"/> — no hardcoded poll cadence in the client body.
/// Defaults are conservative; a live-adapter slice can tune them from configuration.
/// </summary>
public sealed class Mt5BridgeClientOptions
{
    /// <summary>Delay between <c>symbol_info_tick</c> polls in <c>StreamTicksAsync</c>.</summary>
    public TimeSpan TickPollInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Delay between last-closed-bar polls in <c>StreamCandlesAsync</c>.</summary>
    public TimeSpan CandlePollInterval { get; init; } = TimeSpan.FromSeconds(5);
}
