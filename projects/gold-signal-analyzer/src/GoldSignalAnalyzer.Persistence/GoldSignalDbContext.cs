using GoldSignalAnalyzer.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSignalAnalyzer.Persistence;

/// <summary>
/// EF Core 8 SQLite context (arch §6). Representative tables only this cycle: AppSettings,
/// SignalHistory, PaperTrade. No secret columns anywhere (NFR-1).
/// </summary>
public sealed class GoldSignalDbContext : DbContext
{
    public GoldSignalDbContext(DbContextOptions<GoldSignalDbContext> options) : base(options)
    {
    }

    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<SignalHistoryRecord> SignalHistory => Set<SignalHistoryRecord>();
    public DbSet<PaperTradeRecord> PaperTrades => Set<PaperTradeRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSetting>(e =>
        {
            e.ToTable("AppSettings");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Key).IsRequired();
            e.Property(x => x.Value).IsRequired();
        });

        modelBuilder.Entity<SignalHistoryRecord>(e =>
        {
            e.ToTable("SignalHistory");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TimestampUtc);
        });

        modelBuilder.Entity<PaperTradeRecord>(e =>
        {
            e.ToTable("PaperTrade");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OpenedUtc);
        });
    }
}
