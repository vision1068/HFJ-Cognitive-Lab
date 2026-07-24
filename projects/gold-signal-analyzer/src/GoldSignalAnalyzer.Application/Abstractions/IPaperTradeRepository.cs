namespace GoldSignalAnalyzer.Application.Abstractions;

/// <summary>Repository over the PaperTrade journal table (schema stub this cycle).</summary>
public interface IPaperTradeRepository
{
    Task<int> CountAsync(CancellationToken ct = default);
}
