using GoldSignalAnalyzer.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GoldSignalAnalyzer.Persistence.Repositories;

public sealed class SignalHistoryRepository : ISignalHistoryRepository
{
    private readonly GoldSignalDbContext _db;

    public SignalHistoryRepository(GoldSignalDbContext db) => _db = db;

    public Task<int> CountAsync(CancellationToken ct = default) => _db.SignalHistory.CountAsync(ct);
}
