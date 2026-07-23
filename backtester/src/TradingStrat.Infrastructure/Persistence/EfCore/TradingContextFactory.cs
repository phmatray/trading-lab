using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TradingStrat.Infrastructure.Persistence.EfCore;

/// <summary>
/// Design-time factory for creating TradingContext instances during EF Core migrations.
/// This enables 'dotnet ef migrations' commands to work without running the full application.
/// </summary>
public class TradingContextFactory : IDesignTimeDbContextFactory<TradingContext>
{
    public TradingContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<TradingContext> optionsBuilder = new DbContextOptionsBuilder<TradingContext>();
        optionsBuilder.UseSqlite("Data Source=trading.db");

        return new TradingContext(optionsBuilder.Options);
    }
}
