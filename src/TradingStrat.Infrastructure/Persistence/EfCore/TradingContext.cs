using Microsoft.EntityFrameworkCore;
using TradingStrat.Domain.Entities;
using TradingStrat.Infrastructure.Persistence.EventStore;

namespace TradingStrat.Infrastructure.Persistence.EfCore;

public class TradingContext : DbContext
{
    public TradingContext(DbContextOptions<TradingContext> options) : base(options)
    {
    }

    public DbSet<HistoricalPrice> HistoricalPrices { get; set; } = null!;
    public DbSet<Security> Securities { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public DbSet<StrategyRecommendation> StrategyRecommendations { get; set; } = null!;
    public DbSet<Portfolio> Portfolios { get; set; } = null!;
    public DbSet<Position> Positions { get; set; } = null!;
    public DbSet<PortfolioCashTransaction> CashTransactions { get; set; } = null!;
    public DbSet<CustomStrategy> CustomStrategies { get; set; } = null!;
    public DbSet<BacktestRun> BacktestRuns { get; set; } = null!;
    public DbSet<ActivityEvent> ActivityEvents { get; set; } = null!;

    // Event Sourcing
    public DbSet<EventRecord> Events { get; set; } = null!;
    public DbSet<SnapshotRecord> Snapshots { get; set; } = null!;

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

            entity.Property(e => e.TimeFrame)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(10);

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

            // Unique constraint on Ticker, TimeFrame, and DateTime
            entity.HasIndex(e => new { e.Ticker, e.TimeFrame, e.DateTime })
                .IsUnique()
                .HasDatabaseName("IX_HistoricalPrices_Ticker_TimeFrame_DateTime");

            // Index for fast timeframe queries
            entity.HasIndex(e => new { e.Ticker, e.TimeFrame })
                .HasDatabaseName("IX_HistoricalPrices_Ticker_TimeFrame");

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

        // Configure ChatMessage entity
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SessionId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.Ticker)
                .HasMaxLength(50);

            entity.Property(e => e.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Index for efficient conversation history queries
            entity.HasIndex(e => new { e.SessionId, e.Timestamp })
                .HasDatabaseName("IX_ChatMessages_Session_Timestamp");
        });

        // Configure StrategyRecommendation entity
        modelBuilder.Entity<StrategyRecommendation>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Ticker)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.StrategyType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Summary)
                .IsRequired();

            entity.Property(e => e.Recommendation)
                .IsRequired();

            entity.Property(e => e.Confidence)
                .HasPrecision(5, 2);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Store ActionItems as JSON
            entity.Property(e => e.ActionItems)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<ActionItem>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<ActionItem>())
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<ActionItem>>(
                    (c1, c2) => System.Text.Json.JsonSerializer.Serialize(c1, (System.Text.Json.JsonSerializerOptions?)null) == System.Text.Json.JsonSerializer.Serialize(c2, (System.Text.Json.JsonSerializerOptions?)null),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => System.Text.Json.JsonSerializer.Deserialize<List<ActionItem>>(
                        System.Text.Json.JsonSerializer.Serialize(c, (System.Text.Json.JsonSerializerOptions?)null),
                        (System.Text.Json.JsonSerializerOptions?)null) ?? new List<ActionItem>()));

            // Index for finding recent recommendations by ticker
            entity.HasIndex(e => new { e.Ticker, e.CreatedAt })
                .HasDatabaseName("IX_StrategyRecommendations_Ticker_CreatedAt");
        });

        // Configure Portfolio entity
        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.Cash)
                .HasPrecision(18, 2);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.LastUpdated)
                .IsRequired();

            // Index on Name for lookup
            entity.HasIndex(e => e.Name)
                .HasDatabaseName("IX_Portfolios_Name");
        });

        // Configure Position entity
        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Ticker)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.EntryPrice)
                .HasPrecision(18, 6);

            entity.Property(e => e.EntryDate)
                .IsRequired();

            entity.Property(e => e.Notes)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.LastUpdated)
                .IsRequired();

            // Relationship: Position belongs to Portfolio
            entity.HasOne(e => e.Portfolio)
                .WithMany(p => p.Positions)
                .HasForeignKey(e => e.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: one ticker per portfolio
            entity.HasIndex(e => new { e.PortfolioId, e.Ticker })
                .IsUnique()
                .HasDatabaseName("IX_Positions_Portfolio_Ticker");
        });

        // Configure PortfolioCashTransaction entity
        modelBuilder.Entity<PortfolioCashTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);

            entity.Property(e => e.TransactionDate)
                .IsRequired();

            entity.Property(e => e.Notes)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Relationship: Transaction belongs to Portfolio
            entity.HasOne(e => e.Portfolio)
                .WithMany()
                .HasForeignKey(e => e.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for finding transactions by portfolio and date
            entity.HasIndex(e => new { e.PortfolioId, e.TransactionDate })
                .HasDatabaseName("IX_CashTransactions_Portfolio_Date");
        });

        // Configure CustomStrategy entity
        modelBuilder.Entity<CustomStrategy>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.Author)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.DefinitionJson)
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.LastUpdatedAt)
                .IsRequired();

            entity.Property(e => e.LastBacktestReturn)
                .HasPrecision(18, 4);

            // Index for filtering by category
            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_CustomStrategies_Category");

            // Index for finding user's strategies sorted by date
            entity.HasIndex(e => new { e.Author, e.CreatedAt })
                .HasDatabaseName("IX_CustomStrategies_Author_CreatedAt");
        });

        // Configure EventRecord entity (Event Sourcing)
        modelBuilder.Entity<EventRecord>(entity =>
        {
            // Composite primary key: StreamId + Version
            entity.HasKey(e => new { e.StreamId, e.Version });

            entity.Property(e => e.StreamId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Version)
                .IsRequired();

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.EventData)
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property(e => e.Timestamp)
                .IsRequired();

            // Index for efficient event stream queries
            entity.HasIndex(e => new { e.StreamId, e.Version })
                .HasDatabaseName("IX_Events_StreamId_Version");
        });

        // Configure SnapshotRecord entity (Event Sourcing)
        modelBuilder.Entity<SnapshotRecord>(entity =>
        {
            entity.HasKey(e => e.AggregateId);

            entity.Property(e => e.AggregateId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.AggregateType)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Version)
                .IsRequired();

            entity.Property(e => e.SnapshotData)
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property(e => e.CreatedAt)
                .IsRequired();
        });

        // Configure BacktestRun entity
        modelBuilder.Entity<BacktestRun>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Ticker)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.StrategyType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.StrategyName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.StrategyParametersJson)
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property(e => e.ConfigJson)
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property(e => e.ResultsJson)
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property(e => e.ExecutedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.ExecutionTimeMs)
                .IsRequired();

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(1000);

            entity.Property(e => e.Tags)
                .HasMaxLength(500);

            // Index for filtering by ticker
            entity.HasIndex(e => e.Ticker)
                .HasDatabaseName("IX_BacktestRuns_Ticker");

            // Index for sorting by execution date
            entity.HasIndex(e => e.ExecutedAt)
                .HasDatabaseName("IX_BacktestRuns_ExecutedAt");

            // Index for filtering by strategy type
            entity.HasIndex(e => e.StrategyType)
                .HasDatabaseName("IX_BacktestRuns_StrategyType");
        });

        // Configure ActivityEvent entity
        modelBuilder.Entity<ActivityEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.EntityId);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.Metadata)
                .HasColumnType("TEXT");

            entity.Property(e => e.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Index for sorting by timestamp (descending)
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_ActivityEvents_Timestamp");

            // Index for filtering by event type
            entity.HasIndex(e => e.EventType)
                .HasDatabaseName("IX_ActivityEvents_EventType");
        });
    }
}
