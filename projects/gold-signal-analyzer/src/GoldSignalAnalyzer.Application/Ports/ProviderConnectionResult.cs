namespace GoldSignalAnalyzer.Application.Ports;

/// <summary>
/// Outcome of <c>IMarketDataProvider.ConnectAsync</c> (spec §9 <c>ProviderConnectionResult</c>;
/// replaces the Cycle-1 bare <c>ConnectionStatus</c> return). Wraps the honest connection
/// <see cref="ConnectionStatus"/> plus an optional human-readable message. No fabricated data implied.
/// </summary>
public sealed record ProviderConnectionResult(ConnectionStatus Status, string? Message = null);
