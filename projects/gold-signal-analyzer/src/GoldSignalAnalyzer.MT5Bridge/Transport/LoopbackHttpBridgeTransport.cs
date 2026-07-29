using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GoldSignalAnalyzer.MT5Bridge.Exceptions;
using GoldSignalAnalyzer.MT5Bridge.Protocol;

namespace GoldSignalAnalyzer.MT5Bridge.Transport;

/// <summary>
/// Loopback-only HTTP transport to the Python bridge (ADR-14).
///
/// LOOPBACK BY CONSTRUCTION: the base address is built from a COMPILE-TIME host literal
/// (<see cref="LoopbackHost"/> = "127.0.0.1") plus the handshake integer <c>port</c> only —
/// <c>new Uri($"http://127.0.0.1:{port}/")</c>. There is NO public constructor parameter, property,
/// setter, or config binding that accepts a host, hostname, or full URI, and no member takes an
/// <see cref="HttpClient"/>/<see cref="HttpMessageHandler"/>. Non-loopback is therefore not merely
/// rejected — it is UNREPRESENTABLE.
///
/// NO LOGGING HANDLER (C10.2): the <see cref="HttpClient"/> is constructed with a bare default
/// handler (<c>new HttpClient()</c>) and no <see cref="DelegatingHandler"/> can be injected, so no
/// request/header logging handler can observe the <c>Authorization</c> header. A reflection test
/// asserts the handler pipeline contains zero <see cref="DelegatingHandler"/>s.
/// </summary>
public sealed class LoopbackHttpBridgeTransport : IBridgeTransport
{
    /// <summary>The ONLY host this transport will ever contact. Not configurable (ADR-14).</summary>
    private const string LoopbackHost = "127.0.0.1";

    private readonly HttpClient _client = new HttpClient();
    private string? _token;
    private bool _bound;

    public void Bind(int port, string token)
    {
        if (port <= 0)
            throw new ArgumentOutOfRangeException(nameof(port), port, "Bridge port must be positive.");
        _token = token ?? throw new ArgumentNullException(nameof(token));

        // Host is the literal; only the integer port varies. A malicious handshake cannot supply a
        // host here — Bind has no host parameter (ADR-14 / AC-48.3).
        _client.BaseAddress = new Uri($"http://{LoopbackHost}:{port}/");
        _bound = true;
    }

    /// <summary>The bound base address — exposed so tests can assert Host=="127.0.0.1" &amp; IsLoopback.</summary>
    public Uri? BaseAddress => _client.BaseAddress;

    public async Task<BridgeResponse> GetHealthAsync(CancellationToken cancellationToken)
    {
        EnsureBound();
        using var request = new HttpRequestMessage(HttpMethod.Get, "health");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        using var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BridgeResponse> SendAsync(
        string command,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        EnsureBound();
        var payload = new BridgeRequest { Command = command, Params = parameters };
        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        using var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }

    // The server returns a JSON body on EVERY status (200 ok, and 4xx {ok:false,error}); the error
    // code — not the HTTP status — drives the client's typed-exception mapping (C7), so we parse the
    // body regardless of status rather than throwing on non-2xx.
    private static async Task<BridgeResponse> ReadResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        try
        {
            var parsed = JsonSerializer.Deserialize<BridgeResponse>(body);
            if (parsed is null)
                throw new BridgeProtocolException("Bridge returned an empty/null response body.");
            return parsed;
        }
        catch (JsonException ex)
        {
            throw new BridgeProtocolException($"Bridge returned an unparseable response body: {ex.Message}");
        }
    }

    private void EnsureBound()
    {
        if (!_bound)
            throw new InvalidOperationException("Transport is not bound. Call Bind(port, token) first (via ConnectAsync).");
    }

    public void Dispose() => _client.Dispose();
}
