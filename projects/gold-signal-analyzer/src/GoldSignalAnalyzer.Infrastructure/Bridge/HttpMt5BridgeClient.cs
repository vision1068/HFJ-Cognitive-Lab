using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Bridge;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Infrastructure.Bridge;

/// <summary>
/// FR-8 / NFR-2: HTTP client for the local Python MT5 bridge. Enforces:
///   * loopback-only target (re-validated here even though BridgeEndpoint
///     already guarantees it — defense in depth),
///   * a bearer token on EVERY request (X-Bridge-Token header),
///   * heartbeat that degrades to "unhealthy" instead of throwing (NFR-4).
/// The token is supplied at construction from an OS-protected source
/// (never from source/appsettings — NFR-1).
/// </summary>
public sealed class HttpMt5BridgeClient : IMt5BridgeClient
{
    public const string TokenHeader = "X-Bridge-Token";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly string _token;

    public BridgeEndpoint Endpoint { get; }

    public HttpMt5BridgeClient(HttpClient http, BridgeEndpoint endpoint, string token)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Bridge token is required (bridge rejects unauthenticated calls).", nameof(token));
        if (!BridgeEndpoint.IsLoopback(endpoint.BaseUri))
            throw new InvalidOperationException("Refusing to use a non-loopback bridge endpoint (NFR-2).");
        _token = token;
        _http.BaseAddress ??= endpoint.BaseUri;
    }

    private HttpRequestMessage NewRequest(HttpMethod method, string relativeUri)
    {
        var req = new HttpRequestMessage(method, relativeUri);
        req.Headers.TryAddWithoutValidation(TokenHeader, _token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return req;
    }

    public async Task<BridgeHealth> CheckHealthAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        try
        {
            using var resp = await _http.SendAsync(NewRequest(HttpMethod.Get, "health"), ct).ConfigureAwait(false);
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                return BridgeHealth.Unhealthy(now, "unauthorized (token rejected by bridge)");
            if (!resp.IsSuccessStatusCode)
                return BridgeHealth.Unhealthy(now, $"bridge returned {(int)resp.StatusCode}");
            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var dto = JsonSerializer.Deserialize<HealthDto>(body, Json);
            if (dto is null) return BridgeHealth.Unhealthy(now, "empty health response");
            return new BridgeHealth(
                IsHealthy: string.Equals(dto.Status, "ok", StringComparison.OrdinalIgnoreCase),
                TerminalConnected: dto.TerminalConnected,
                CheckedAtUtc: now,
                Detail: dto.Build);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // Heartbeat must never throw for an unreachable/unhealthy bridge (NFR-4).
            return BridgeHealth.Unhealthy(now, ex.GetType().Name);
        }
    }

    public async Task<IReadOnlyList<BrokerSymbol>> ListSymbolsAsync(CancellationToken ct = default)
    {
        var dto = await SendJsonAsync<SymbolsDto>(HttpMethod.Get, "symbols", ct).ConfigureAwait(false);
        return (dto?.Symbols ?? new List<SymbolDto>())
            .Select(s => new BrokerSymbol(s.Raw, s.Description))
            .ToList();
    }

    public async Task<MarketTick?> GetTickAsync(string brokerSymbol, NormalizedSymbol normalized, CancellationToken ct = default)
    {
        var uri = $"tick?symbol={Uri.EscapeDataString(brokerSymbol)}";
        var dto = await SendJsonAsync<TickDto>(HttpMethod.Get, uri, ct, allowNotFound: true).ConfigureAwait(false);
        if (dto is null) return null;
        var time = DateTimeOffset.Parse(dto.Time, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        return new MarketTick(normalized, dto.Bid, dto.Ask, time);
    }

    public async Task<IReadOnlyList<Candle>> GetCandlesAsync(string brokerSymbol, NormalizedSymbol normalized, TimeFrame timeFrame, int count, CancellationToken ct = default)
    {
        var uri = $"candles?symbol={Uri.EscapeDataString(brokerSymbol)}&tf={(int)timeFrame}&count={count}";
        var dto = await SendJsonAsync<CandlesDto>(HttpMethod.Get, uri, ct).ConfigureAwait(false);
        var rows = dto?.Candles ?? new List<CandleDto>();
        return rows.Select(c =>
        {
            var time = DateTimeOffset.Parse(c.Time, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            return new Candle(normalized, timeFrame, time, c.Open, c.High, c.Low, c.Close, c.Volume);
        }).ToList();
    }

    private async Task<T?> SendJsonAsync<T>(HttpMethod method, string relativeUri, CancellationToken ct, bool allowNotFound = false)
    {
        using var resp = await _http.SendAsync(NewRequest(method, relativeUri), ct).ConfigureAwait(false);
        if (allowNotFound && resp.StatusCode == HttpStatusCode.NotFound) return default;
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Bridge rejected the request token (NFR-2).");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(body, Json);
    }

    // --- transport DTOs (kept internal to the client) ---
    private sealed record HealthDto(string Status, bool TerminalConnected, string? Build);
    private sealed record SymbolsDto(List<SymbolDto> Symbols);
    private sealed record SymbolDto(string Raw, string? Description);
    private sealed record TickDto(string Symbol, decimal Bid, decimal Ask, string Time);
    private sealed record CandlesDto(List<CandleDto> Candles);
    private sealed record CandleDto(string Time, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);
}
