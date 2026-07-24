using GoldSignalAnalyzer.Application.Abstractions;
using GoldSignalAnalyzer.Application.Security;
using GoldSignalAnalyzer.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSignalAnalyzer.Persistence.Repositories;

public sealed class AppSettingsRepository : IAppSettingsRepository
{
    private readonly GoldSignalDbContext _db;

    public AppSettingsRepository(GoldSignalDbContext db) => _db = db;

    public async Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        // Gate B3: the key/value table must never carry a secret. Reject denylisted keys at write time.
        if (SecretDenylist.IsSecret(key))
            throw new SecretPersistenceException(key);

        var existing = await _db.AppSettings.FirstOrDefaultAsync(x => x.Key == key, ct);
        if (existing is null)
        {
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
        else
        {
            existing.Value = value;
            existing.ModifiedOn = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
        => (await _db.AppSettings.FirstOrDefaultAsync(x => x.Key == key, ct))?.Value;
}
