using Microsoft.EntityFrameworkCore;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Infrastructure.Persistence.EfCore;

public class TradingContext : DbContext
{
    public TradingContext(DbContextOptions<TradingContext> options) : base(options)
    {
    }

    public DbSet<HistoricalPrice> HistoricalPrices { get; set; } = null!;
    public DbSet<Security> Securities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure HistoricalPrice entity
        modelBuilder.Entity<HistoricalPrice>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Ticker)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ISIN)
                .HasMaxLength(12);

            entity.Property(e => e.DateTime)
                .IsRequired();

            entity.Property(e => e.Open)
                .HasPrecision(18, 6);

            entity.Property(e => e.High)
                .HasPrecision(18, 6);

            entity.Property(e => e.Low)
                .HasPrecision(18, 6);

            entity.Property(e => e.Close)
                .HasPrecision(18, 6);

            entity.Property(e => e.AdjustedClose)
                .HasPrecision(18, 6);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraint on Ticker and DateTime
            entity.HasIndex(e => new { e.Ticker, e.DateTime })
                .IsUnique()
                .HasDatabaseName("IX_HistoricalPrices_Ticker_DateTime");

            // Index for ISIN lookups
            entity.HasIndex(e => e.ISIN)
                .HasDatabaseName("IX_HistoricalPrices_ISIN");
        });

        // Configure Security entity
        modelBuilder.Entity<Security>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Ticker)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ISIN)
                .HasMaxLength(12);

            entity.Property(e => e.Name)
                .HasMaxLength(255);

            entity.Property(e => e.SecurityType)
                .HasMaxLength(50);

            entity.Property(e => e.Exchange)
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraint on Ticker
            entity.HasIndex(e => e.Ticker)
                .IsUnique()
                .HasDatabaseName("IX_Securities_Ticker");

            // Unique constraint on ISIN
            entity.HasIndex(e => e.ISIN)
                .IsUnique()
                .HasDatabaseName("IX_Securities_ISIN");
        });
    }
}
