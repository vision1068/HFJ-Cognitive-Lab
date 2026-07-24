namespace GoldSignalAnalyzer.Application.Abstractions;

/// <summary>Repository over the SignalHistory table (schema stub this cycle).</summary>
public interface ISignalHistoryRepository
{
    Task<int> CountAsync(CancellationToken ct = default);
}
