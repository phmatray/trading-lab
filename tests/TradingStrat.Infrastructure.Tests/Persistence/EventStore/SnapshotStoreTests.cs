using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradingStrat.Domain.Entities;
using TradingStrat.Infrastructure.Persistence.EfCore;

namespace TradingStrat.Infrastructure.Tests.Persistence.EventStore;

/// <summary>
/// Tests for SnapshotStore implementation.
/// Verifies snapshot persistence, retrieval, and optimization behavior.
/// </summary>
public class SnapshotStoreTests : IDisposable
{
    private readonly TradingContext _context;
    private readonly SnapshotStore _snapshotStore;

    public SnapshotStoreTests()
    {
        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<TradingContext>()
            .UseInMemoryDatabase($"SnapshotStoreTests_{Guid.NewGuid()}")
            .Options;

        _context = new TradingContext(options);
        _snapshotStore = new SnapshotStore(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region SaveSnapshotAsync Tests

    [Fact]
    public async Task SaveSnapshotAsync_StoresCurrentState()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 1,
            Name = "Test Portfolio",
            Cash = 10000m,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Positions = new List<Position>
            {
                new Position
                {
                    Id = 1,
                    PortfolioId = 1,
                    Ticker = "AAPL",
                    Quantity = 10,
                    EntryPrice = 150m,
                    EntryDate = DateTime.Today.AddDays(-1)
                }
            }
        };

        // Manually set version for testing
        typeof(Portfolio).GetProperty("Version")!.SetValue(portfolio, 5);

        // Act
        await _snapshotStore.SaveSnapshotAsync(portfolio);

        // Assert
        var snapshot = await _context.Snapshots.FirstOrDefaultAsync(s => s.AggregateId == "1");
        snapshot.ShouldNotBeNull();
        snapshot.Version.ShouldBe(5);
        snapshot.AggregateType.ShouldContain("Portfolio");
        snapshot.SnapshotData.ShouldContain("Test Portfolio");
        snapshot.SnapshotData.ShouldContain("AAPL");
    }

    [Fact]
    public async Task SaveSnapshotAsync_UpdatesExistingSnapshot()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 1,
            Name = "Test Portfolio",
            Cash = 10000m,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        typeof(Portfolio).GetProperty("Version")!.SetValue(portfolio, 5);

        // Save first snapshot
        await _snapshotStore.SaveSnapshotAsync(portfolio);

        // Modify portfolio
        portfolio.Cash = 15000m;
        typeof(Portfolio).GetProperty("Version")!.SetValue(portfolio, 10);

        // Act - Save updated snapshot
        await _snapshotStore.SaveSnapshotAsync(portfolio);

        // Assert
        var snapshots = await _context.Snapshots.Where(s => s.AggregateId == "1").ToListAsync();
        snapshots.Count.ShouldBe(1); // Should replace, not add
        snapshots[0].Version.ShouldBe(10);
        snapshots[0].SnapshotData.ShouldContain("15000");
    }

    [Fact]
    public async Task SaveSnapshotAsync_WithNullAggregate_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _snapshotStore.SaveSnapshotAsync<Portfolio>(null!)
        );
    }

    #endregion

    #region GetSnapshotAsync Tests

    [Fact]
    public async Task GetSnapshotAsync_RestoresStateCorrectly()
    {
        // Arrange
        var originalPortfolio = new Portfolio
        {
            Id = 1,
            Name = "Test Portfolio",
            Cash = 10000m,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Positions = new List<Position>
            {
                new Position
                {
                    Id = 1,
                    PortfolioId = 1,
                    Ticker = "AAPL",
                    Quantity = 10,
                    EntryPrice = 150m,
                    EntryDate = DateTime.Today.AddDays(-1)
                }
            }
        };
        typeof(Portfolio).GetProperty("Version")!.SetValue(originalPortfolio, 5);
        await _snapshotStore.SaveSnapshotAsync(originalPortfolio);

        // Act
        var snapshot = await _snapshotStore.GetSnapshotAsync<Portfolio>("1");

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.Version.ShouldBe(5);
        snapshot.Aggregate.Id.ShouldBe(1);
        snapshot.Aggregate.Name.ShouldBe("Test Portfolio");
        snapshot.Aggregate.Cash.ShouldBe(10000m);
        snapshot.Aggregate.Positions.Count.ShouldBe(1);
        snapshot.Aggregate.Positions[0].Ticker.ShouldBe("AAPL");
    }

    [Fact]
    public async Task GetSnapshotAsync_ForNonExistentAggregate_ReturnsNull()
    {
        // Act
        var snapshot = await _snapshotStore.GetSnapshotAsync<Portfolio>("non-existent");

        // Assert
        snapshot.ShouldBeNull();
    }

    #endregion

    #region DeleteSnapshotsAsync Tests

    [Fact]
    public async Task DeleteSnapshotsAsync_RemovesAllSnapshots()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 1,
            Name = "Test Portfolio",
            Cash = 10000m,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        typeof(Portfolio).GetProperty("Version")!.SetValue(portfolio, 5);
        await _snapshotStore.SaveSnapshotAsync(portfolio);

        // Verify snapshot exists
        var snapshotsBefore = await _context.Snapshots.Where(s => s.AggregateId == "1").ToListAsync();
        snapshotsBefore.ShouldNotBeEmpty();

        // Act
        await _snapshotStore.DeleteSnapshotsAsync("1");

        // Assert
        var snapshotsAfter = await _context.Snapshots.Where(s => s.AggregateId == "1").ToListAsync();
        snapshotsAfter.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteSnapshotsAsync_ForNonExistentAggregate_DoesNothing()
    {
        // Act & Assert - Should not throw
        await _snapshotStore.DeleteSnapshotsAsync("non-existent");
    }

    #endregion

    #region SnapshotExistsAsync Tests

    [Fact]
    public async Task SnapshotExistsAsync_ForExistingSnapshot_ReturnsTrue()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 1,
            Name = "Test Portfolio",
            Cash = 10000m,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        typeof(Portfolio).GetProperty("Version")!.SetValue(portfolio, 5);
        await _snapshotStore.SaveSnapshotAsync(portfolio);

        // Act
        bool exists = await _snapshotStore.SnapshotExistsAsync("1");

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task SnapshotExistsAsync_ForNonExistentSnapshot_ReturnsFalse()
    {
        // Act
        bool exists = await _snapshotStore.SnapshotExistsAsync("non-existent");

        // Assert
        exists.ShouldBeFalse();
    }

    #endregion
}
