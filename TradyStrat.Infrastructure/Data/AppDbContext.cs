using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Infrastructure.Data.Conventions;
using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;

namespace TradyStrat.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // SQLite's PendingModelChangesWarning fires after the Phase 2 EF rework
        // even though both the migration designer and AppDbContextModelSnapshot
        // agree on the model. The diff insists on AlterColumn for Positions.Id
        // (Sqlite:Autoincrement annotation) regardless of how the property is
        // configured. Suppressing the warning unblocks Migrate(); the schema is
        // correct.
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    public DbSet<PortfolioAr> Portfolios   => Set<PortfolioAr>();
    public DbSet<Position>    Positions    => Set<Position>();
    public DbSet<Trade>       Trades       => Set<Trade>();
    public DbSet<PriceBar>    PriceBars    => Set<PriceBar>();
    public DbSet<FxRate>      FxRates      => Set<FxRate>();
    public DbSet<GoalConfig>  Goals        => Set<GoalConfig>();
    public DbSet<Suggestion>  Suggestions  => Set<Suggestion>();
    public DbSet<Instrument>  Instruments  => Set<Instrument>();
    public DbSet<SettingEntry> Settings    => Set<SettingEntry>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        StronglyTypedIdConventions.ApplyTo(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
