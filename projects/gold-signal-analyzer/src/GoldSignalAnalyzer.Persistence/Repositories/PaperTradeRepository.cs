using GoldSignalAnalyzer.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GoldSignalAnalyzer.Persistence.Repositories;

public sealed class PaperTradeRepository : IPaperTradeRepository
{
    private readonly GoldSignalDbContext _db;

    public PaperTradeRepository(GoldSignalDbContext db) => _db = db;

    public Task<int> CountAsync(CancellationToken ct = default) => _db.PaperTrades.CountAsync(ct);
}
