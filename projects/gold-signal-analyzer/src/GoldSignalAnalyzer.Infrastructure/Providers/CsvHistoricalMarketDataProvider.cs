using System.Globalization;
using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Domain;

namespace GoldSignalAnalyzer.Infrastructure.Providers;

/// <summary>
/// FR-10: an <see cref="IMarketDataProvider"/> backed by a historical CSV file
/// (for backtest/replay). <see cref="IsLive"/> is false — its output must never
/// be treated as a live quote (FR-11). CSV format (with header):
/// <c>time,open,high,low,close,volume</c>, time as ISO-8601 UTC.
/// </summary>
public sealed class CsvHistoricalMarketDataProvider : IMarketDataProvider
{
    private readonly NormalizedSymbol _symbol;
    private readonly TimeFrame _timeFrame;
    private readonly IReadOnlyList<Candle> _candles;

    public string Name => "CsvHistorical";
    public bool IsLive => false;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public CsvHistoricalMarketDataProvider(NormalizedSymbol symbol, TimeFrame timeFrame, IReadOnlyList<Candle> candles)
    {
        _symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        _timeFrame = timeFrame;
        _candles = candles ?? throw new ArgumentNullException(nameof(candles));
    }

    public static CsvHistoricalMarketDataProvider LoadFromFile(NormalizedSymbol symbol, TimeFrame tf, string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("CSV not found.", path);
        return ParseCsv(symbol, tf, File.ReadAllLines(path));
    }

    public static CsvHistoricalMarketDataProvider ParseCsv(NormalizedSymbol symbol, TimeFrame tf, IEnumerable<string> lines)
    {
        var candles = new List<Candle>();
        bool first = true;
        foreach (var raw in lines)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var line = raw.Trim();
            if (first)
            {
                first = false;
                if (line.StartsWith("time", StringComparison.OrdinalIgnoreCase)) continue; // header
            }
            var parts = line.Split(',');
            if (parts.Length < 6)
                throw new FormatException($"Malformed candle row (need 6 columns): '{line}'");
            var time = DateTimeOffset.Parse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            decimal D(int i) => decimal.Parse(parts[i], CultureInfo.InvariantCulture);
            candles.Add(new Candle(symbol, tf, time, D(1), D(2), D(3), D(4), D(5)));
        }
        candles.Sort((a, b) => a.OpenTimeUtc.CompareTo(b.OpenTimeUtc));
        return new CsvHistoricalMarketDataProvider(symbol, tf, candles);
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
        => Task.FromResult<IReadOnlyList<BrokerSymbol>>(new[] { new BrokerSymbol(_symbol.Value) });

    public Task<MarketTick?> GetLatestTickAsync(NormalizedSymbol symbol, CancellationToken ct = default)
    {
        if (State != ConnectionState.Connected || _candles.Count == 0)
            return Task.FromResult<MarketTick?>(null);
        var last = _candles[^1];
        // Historical close reconstructed as a zero-spread tick — explicitly NOT live.
        var tick = new MarketTick(symbol, last.Close, last.Close, last.OpenTimeUtc);
        return Task.FromResult<MarketTick?>(tick);
    }

    public Task<IReadOnlyList<Candle>> GetCandlesAsync(NormalizedSymbol symbol, TimeFrame timeFrame, int count, CancellationToken ct = default)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        IReadOnlyList<Candle> slice = _candles.TakeLast(count).ToList();
        return Task.FromResult(slice);
    }
}
