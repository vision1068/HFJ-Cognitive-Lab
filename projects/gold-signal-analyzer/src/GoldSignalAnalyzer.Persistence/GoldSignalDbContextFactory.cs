using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GoldSignalAnalyzer.Persistence;

/// <summary>
/// Design-time factory (arch §6) so the EF CLI can create migrations headlessly, without booting
/// WPF. Uses a throwaway design-time path; never touches the real AppData DB.
/// </summary>
public sealed class GoldSignalDbContextFactory : IDesignTimeDbContextFactory<GoldSignalDbContext>
{
    public GoldSignalDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<GoldSignalDbContext>()
            .UseSqlite("Data Source=gsa_designtime.db")
            .Options;
        return new GoldSignalDbContext(options);
    }
}
