using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Exceptions;
using GoldSignalAnalyzer.Application.Ports;
using GoldSignalAnalyzer.Domain.Entities;
using GoldSignalAnalyzer.Domain.Enums;
using GoldSignalAnalyzer.Domain.ValueObjects;
using GoldSignalAnalyzer.MT5Bridge.Exceptions;
using GoldSignalAnalyzer.MT5Bridge.Process;
using GoldSignalAnalyzer.MT5Bridge.Protocol;
using GoldSignalAnalyzer.MT5Bridge.Transport;

namespace GoldSignalAnalyzer.MT5Bridge;

/// <summary>
/// Loopback-HTTP client to the read-only Python bridge, implementing the byte-exact §9
/// <see cref="IMarketDataProvider"/> over an injectable transport seam
/// (<see cref="IBridgeTransport"/>) and process seam (<see cref="IBridgeProcess"/>) plus
/// <see cref="ICredentialStore"/> and <see cref="IClock"/> (FR-47/FR-48).
///
/// This slice drives the bridge but attaches NO live terminal: <c>run_bridge.py</c> never calls
/// <c>connect()</c> and there is no <c>connect</c>/<c>initialize</c>/<c>login</c> command, so every
/// live read returns <c>handler_error</c> ("bridge up, terminal not attached"). The client is HONEST
/// about that — reads THROW on any non-<c>ok</c> reply (never fabricate / never honest-empty on an
/// error), streams THROW mid-enumeration, and every field mapping is GUARDED (a missing key throws,
/// never defaults to 0/0m).
///
/// UNVERIFIED-UNTIL-LIVE (confirm-item C-live): the exact <c>symbol_info</c>/rate field names below
/// (<c>trade_contract_size</c>, <c>tick_volume</c>, …) are asserted only against fixtures this slice;
/// their correctness against a REAL Exness terminal is unverifiable in-environment. The guards make a
/// wrong mapping FAIL LOUD, not fabricate a zero.
/// </summary>
public sealed class Mt5BridgeClient : IMarketDataProvider, IDisposable, IAsyncDisposable
{
    /// <summary>Credential-store key for the bridge Bearer token.</summary>
    public const string TokenKey = "gsa:mt5-bridge-token";

    /// <summary>Provider display name (AC-47.5). A constant — issues no request.</summary>
    public const string ProviderNameConstant = "MT5 Bridge (read-only)";

    private static readonly IReadOnlyDictionary<string, object?> NoParams
        = new Dictionary<string, object?>();

    private readonly IBridgeProcess _process;
    private readonly IBridgeTransport _transport;
    private readonly ICredentialStore _credentials;
    private readonly IClock _clock;
    private readonly Mt5BridgeClientOptions _options;
    private readonly SemaphoreSlim _connectGate = new(1, 1);

    private ConnectionStatus _status = ConnectionStatus.NotConnected;

    public Mt5BridgeClient(
        IBridgeProcess process,
        IBridgeTransport transport,
        ICredentialStore credentials,
        IClock clock,
        Mt5BridgeClientOptions? options = null)
    {
        _process = process ?? throw new ArgumentNullException(nameof(process));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _options = options ?? new Mt5BridgeClientOptions();
    }

    public string ProviderName => ProviderNameConstant;

    // -- connection ---------------------------------------------------------

    /// <summary>
    /// C1: issues ONLY the process handshake + a single <c>GET /health</c> (zero read/POST commands).
    /// Returns Connected = "bridge transport is live" — it structurally cannot attach a terminal.
    /// </summary>
    public async Task<ProviderConnectionResult> ConnectAsync(CancellationToken cancellationToken)
    {
        await _connectGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var token = await GetOrCreateTokenAsync(cancellationToken).ConfigureAwait(false);

            int port = await _process.StartAsync(token, cancellationToken).ConfigureAwait(false);
            _transport.Bind(port, token);

            var health = await _transport.GetHealthAsync(cancellationToken).ConfigureAwait(false);
            if (!health.Ok)
            {
                _status = ConnectionStatus.Faulted;
                throw MapError(health.Error, health.Detail);
            }

            _status = ConnectionStatus.Connected;
            return new ProviderConnectionResult(
                ConnectionStatus.Connected,
                "Bridge transport up (loopback, authenticated). No live MT5 terminal attached — market reads are gated to a later slice.");
        }
        finally
        {
            _connectGate.Release();
        }
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _status = ConnectionStatus.NotConnected;
        _process.Kill();
        return Task.CompletedTask;
    }

    private async Task<string> GetOrCreateTokenAsync(CancellationToken ct)
    {
        var token = await _credentials.GetSecretAsync(TokenKey, ct).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(token))
            return token;

        // C10.1: cryptographically-secure token, NOT System.Random (guessable by a co-resident
        // process). 32 bytes of CSPRNG output, base64-encoded.
        token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await _credentials.SetSecretAsync(TokenKey, token, ct).ConfigureAwait(false);
        return token;
    }

    // -- account (ADR-13 / C11) --------------------------------------------

    /// <summary>
    /// ADR-13: returns <c>null</c>. The bridge suppresses every monetary field, so a complete
    /// <see cref="AccountSnapshot"/> cannot be honestly sourced read-only — filling Balance/Equity/
    /// FreeMargin with 0m would be fabrication. C11: the client issues NO <c>account_info</c> command
    /// anywhere this slice (this method touches the transport zero times).
    /// </summary>
    public Task<AccountSnapshot?> GetAccountSnapshotAsync(CancellationToken cancellationToken)
        => Task.FromResult<AccountSnapshot?>(null);

    // -- reads (throw on error, never honest-empty on error — ADR-17/C7) ----

    public async Task<IReadOnlyList<MarketSymbol>> GetAvailableSymbolsAsync(CancellationToken cancellationToken)
    {
        EnsureConnected();
        var resp = await _transport.SendAsync(BridgeCommands.SymbolsGet, NoParams, cancellationToken).ConfigureAwait(false);
        EnsureOk(resp);

        var array = RequireArray(resp);
        var list = new List<MarketSymbol>(array.GetArrayLength());
        foreach (var el in array.EnumerateArray())
            list.Add(new MarketSymbol(GetString(el, "name"), GetString(el, "description")));
        return list;
    }

    public async Task<SymbolSpecification> GetSymbolSpecificationAsync(string symbol, CancellationToken cancellationToken)
    {
        EnsureConnected();
        var resp = await _transport.SendAsync(
            BridgeCommands.SymbolInfo,
            new Dictionary<string, object?> { ["symbol"] = symbol },
            cancellationToken).ConfigureAwait(false);
        EnsureOk(resp);

        var d = RequireObject(resp);
        // GUARDED mapping (C8): every key explicit; a missing key THROWS (never 0/0m). Field names
        // are the MT5 symbol_info members — UNVERIFIED-UNTIL-LIVE (C-live).
        return new SymbolSpecification(
            Symbol: symbol,
            ContractSize: GetDecimal(d, "trade_contract_size"),
            TickSize: GetDecimal(d, "trade_tick_size"),
            TickValue: GetDecimal(d, "trade_tick_value"),
            Digits: GetInt(d, "digits"),
            LotStep: GetDecimal(d, "volume_step"),
            MinLot: GetDecimal(d, "volume_min"),
            MaxLot: GetDecimal(d, "volume_max"));
    }

    public async Task<IReadOnlyList<Candle>> GetHistoricalCandlesAsync(
        string symbol, Timeframe timeframe, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var label = ToBridgeTimeframe(timeframe); // AC-47.4: throws before any request for M1/M5/M30
        EnsureConnected();

        var resp = await _transport.SendAsync(
            BridgeCommands.CopyRatesRange,
            new Dictionary<string, object?>
            {
                ["symbol"] = symbol,
                ["timeframe"] = label,
                // MT5 python accepts POSIX seconds for the range bounds. UNVERIFIED-UNTIL-LIVE.
                ["date_from"] = from.ToUnixTimeSeconds(),
                ["date_to"] = to.ToUnixTimeSeconds(),
            },
            cancellationToken).ConfigureAwait(false);
        EnsureOk(resp);

        var array = RequireArray(resp);
        var list = new List<Candle>(array.GetArrayLength());
        foreach (var el in array.EnumerateArray())
            list.Add(MapCandle(el, timeframe));
        return list;
    }

    // -- streams (poll + yield real values; THROW mid-enumeration on error — C6) --

    public async IAsyncEnumerable<MarketTick> StreamTicksAsync(
        string symbol, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        EnsureConnected();
        while (!cancellationToken.IsCancellationRequested)
        {
            var resp = await _transport.SendAsync(
                BridgeCommands.SymbolInfoTick,
                new Dictionary<string, object?> { ["symbol"] = symbol },
                cancellationToken).ConfigureAwait(false);
            EnsureOk(resp); // C6: non-ok THROWS mid-enumeration, never yields a fabricated tick

            var d = RequireObject(resp);
            yield return new MarketTick(
                symbol,
                GetDecimal(d, "bid"),
                GetDecimal(d, "ask"),
                UnixSecondsToUtc(GetLong(d, "time")));

            await Task.Delay(_options.TickPollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<Candle> StreamCandlesAsync(
        string symbol, Timeframe timeframe, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var label = ToBridgeTimeframe(timeframe); // AC-47.4: throws before any request for M1/M5/M30
        EnsureConnected();

        while (!cancellationToken.IsCancellationRequested)
        {
            // C6: pin to the LAST CLOSED bar (start=1). Position 0 is the forming bar and must never
            // be emitted as IsClosed=true — IsClosed is derived honestly from the clock in MapCandle.
            var resp = await _transport.SendAsync(
                BridgeCommands.CopyRatesFromPos,
                new Dictionary<string, object?>
                {
                    ["symbol"] = symbol,
                    ["timeframe"] = label,
                    ["start"] = 1,
                    ["count"] = 1,
                },
                cancellationToken).ConfigureAwait(false);
            EnsureOk(resp); // non-ok THROWS mid-enumeration

            var array = RequireArray(resp);
            foreach (var el in array.EnumerateArray())
                yield return MapCandle(el, timeframe);

            await Task.Delay(_options.CandlePollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    // -- mapping helpers ----------------------------------------------------

    /// <summary>AC-47.4: explicit label map; unsupported timeframes fail LOUD before any request.</summary>
    internal static string ToBridgeTimeframe(Timeframe timeframe) => timeframe switch
    {
        Timeframe.M15 => "M15",
        Timeframe.H1 => "H1",
        Timeframe.H4 => "H4",
        Timeframe.D1 => "D1",
        _ => throw new NotSupportedException(
            $"Timeframe {timeframe} is not supported by the MT5 bridge this slice (supported: M15, H1, H4, D1)."),
    };

    private Candle MapCandle(JsonElement rate, Timeframe timeframe)
    {
        // GUARDED mapping (C8). Volume is sourced from tick_volume: MT5 real_volume is commonly 0 on
        // CFD/forex feeds (Exness), so tick_volume is the honest activity measure. A missing key
        // THROWS. UNVERIFIED-UNTIL-LIVE (C-live).
        var openTimeUtc = UnixSecondsToUtc(GetLong(rate, "time"));
        return new Candle(
            OpenTimeUtc: openTimeUtc,
            Open: GetDecimal(rate, "open"),
            High: GetDecimal(rate, "high"),
            Low: GetDecimal(rate, "low"),
            Close: GetDecimal(rate, "close"),
            Volume: GetDouble(rate, "tick_volume"),
            Timeframe: timeframe,
            IsClosed: IsBarClosed(openTimeUtc, timeframe));
    }

    /// <summary>Honest closed-state: a bar is closed once its whole interval is in the past (NFR-5).</summary>
    private bool IsBarClosed(DateTime openTimeUtc, Timeframe timeframe)
        => openTimeUtc + TimeframeDuration(timeframe) <= _clock.UtcNow;

    internal static TimeSpan TimeframeDuration(Timeframe timeframe) => timeframe switch
    {
        Timeframe.M15 => TimeSpan.FromMinutes(15),
        Timeframe.H1 => TimeSpan.FromHours(1),
        Timeframe.H4 => TimeSpan.FromHours(4),
        Timeframe.D1 => TimeSpan.FromDays(1),
        _ => throw new NotSupportedException($"No duration for timeframe {timeframe}."),
    };

    private static DateTime UnixSecondsToUtc(long seconds)
        => DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;

    // -- error mapping (C7: exhaustive; empty ONLY on genuine ok:true,data:[]) --

    private void EnsureConnected()
    {
        if (_process.HasExited)
        {
            _status = ConnectionStatus.Faulted; // crash/early-exit detected (AC-48.5)
            throw new NotConnectedException("MT5 bridge child process has exited — no live data.");
        }
        if (_status != ConnectionStatus.Connected)
            throw new NotConnectedException("MT5 bridge is not connected. Call ConnectAsync first.");
    }

    private static void EnsureOk(BridgeResponse resp)
    {
        if (resp.Ok) return;
        throw MapError(resp.Error, resp.Detail);
    }

    private static Exception MapError(string? code, string? detail)
    {
        var suffix = string.IsNullOrEmpty(detail) ? string.Empty : $" (detail: {detail})";
        return code switch
        {
            BridgeErrorCodes.Unauthorized
                => new BridgeAuthException($"Bridge rejected the Bearer token (unauthorized).{suffix}"),
            BridgeErrorCodes.ProtocolVersionMismatch
                => new BridgeProtocolException($"Bridge protocol version mismatch.{suffix}"),
            BridgeErrorCodes.CommandNotAllowed
                => new BridgeCommandDeniedException($"Bridge denied the command (not on the read-only allowlist).{suffix}"),
            BridgeErrorCodes.MalformedRequest
                => new BridgeRequestException($"Bridge rejected the request as malformed.{suffix}"),
            BridgeErrorCodes.NotFound
                => new BridgeRequestException($"Bridge route not found.{suffix}"),
            BridgeErrorCodes.HandlerError
                => new NotConnectedException($"Bridge is up but no MT5 terminal is attached — read gated to a later slice.{suffix}"),
            _ => new BridgeProtocolException($"Bridge returned an unrecognized error code '{code}'.{suffix}"),
        };
    }

    // -- guarded JSON accessors (C8: missing/wrong-kind key THROWS, never a fabricated default) --

    private static JsonElement RequireArray(BridgeResponse resp)
    {
        if (resp.Data is not { ValueKind: JsonValueKind.Array } array)
            throw new BridgeMappingException("Bridge ok-reply did not carry a JSON array payload.");
        return array;
    }

    private static JsonElement RequireObject(BridgeResponse resp)
    {
        if (resp.Data is not { ValueKind: JsonValueKind.Object } obj)
            throw new BridgeMappingException("Bridge ok-reply did not carry a JSON object payload.");
        return obj;
    }

    private static JsonElement RequireField(JsonElement obj, string key)
    {
        if (!obj.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null)
            throw new BridgeMappingException($"Bridge payload is missing required field '{key}'.");
        return value;
    }

    private static decimal GetDecimal(JsonElement obj, string key)
    {
        var v = RequireField(obj, key);
        if (v.ValueKind != JsonValueKind.Number || !v.TryGetDecimal(out var d))
            throw new BridgeMappingException($"Bridge field '{key}' is not a numeric value.");
        return d;
    }

    private static double GetDouble(JsonElement obj, string key)
    {
        var v = RequireField(obj, key);
        if (v.ValueKind != JsonValueKind.Number || !v.TryGetDouble(out var d))
            throw new BridgeMappingException($"Bridge field '{key}' is not a numeric value.");
        return d;
    }

    private static int GetInt(JsonElement obj, string key)
    {
        var v = RequireField(obj, key);
        if (v.ValueKind != JsonValueKind.Number || !v.TryGetInt32(out var i))
            throw new BridgeMappingException($"Bridge field '{key}' is not an integer value.");
        return i;
    }

    private static long GetLong(JsonElement obj, string key)
    {
        var v = RequireField(obj, key);
        if (v.ValueKind != JsonValueKind.Number || !v.TryGetInt64(out var l))
            throw new BridgeMappingException($"Bridge field '{key}' is not an integer value.");
        return l;
    }

    private static string GetString(JsonElement obj, string key)
    {
        var v = RequireField(obj, key);
        if (v.ValueKind != JsonValueKind.String)
            throw new BridgeMappingException($"Bridge field '{key}' is not a string value.");
        return v.GetString()!;
    }

    // -- disposal (graceful no-orphan, C5) ----------------------------------

    public void Dispose()
    {
        _process.Kill();
        _process.Dispose();
        _transport.Dispose();
        _connectGate.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _process.Kill();
        await _process.DisposeAsync().ConfigureAwait(false);
        _transport.Dispose();
        _connectGate.Dispose();
    }
}
