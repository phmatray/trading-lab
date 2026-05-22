using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TradyStrat.Infrastructure.Data;

/// <summary>
/// Allows `dotnet ef` to build & instantiate AppDbContext without booting the
/// web host. The connection string is a throwaway in-memory SQLite — only the
/// schema model matters at design time.
/// </summary>
internal sealed class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // TRADYSTRAT_DB env var lets `dotnet ef database update` target a specific
        // file (e.g. a /tmp copy during a migration test). Default is :memory:,
        // which is enough for `dotnet ef migrations add`.
        var path = Environment.GetEnvironmentVariable("TRADYSTRAT_DB");
        var connStr = string.IsNullOrWhiteSpace(path)
            ? "Data Source=:memory:"
            : $"Data Source={path}";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connStr)
            .Options;
        return new AppDbContext(options);
    }
}
