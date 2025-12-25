using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Events;
using TradingStrat.Domain.Exceptions;
using TradingStrat.Infrastructure.Persistence.EfCore;

namespace TradingStrat.Infrastructure.Tests.Persistence.EventStore;

/// <summary>
/// Tests for EventStoreRepository implementation.
/// Verifies event persistence, retrieval, versioning, and concurrency control.
/// </summary>
public class EventStoreRepositoryTests : IDisposable
{
    private readonly TradingContext _context;
    private readonly EventStoreRepository _eventStore;
    private const string TestStreamId = "portfolio-123";

    public EventStoreRepositoryTests()
    {
        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<TradingContext>()
            .UseInMemoryDatabase($"EventStoreTests_{Guid.NewGuid()}")
            .Options;

        _context = new TradingContext(options);
        _eventStore = new EventStoreRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region AppendEventsAsync Tests

    [Fact]
    public async Task AppendEventsAsync_WithValidEvents_SavesSuccessfully()
    {
        // Arrange
        var events = new List<DomainEvent>
        {
            new PortfolioCreatedEvent(1, "Test Portfolio", 10000m),
            new PositionAddedEvent(1, "AAPL", 10, 150m, DateTime.UtcNow)
        };

        // Act
        await _eventStore.AppendEventsAsync(TestStreamId, events, expectedVersion: 0);

        // Assert
        var storedEvents = await _context.Events.Where(e => e.StreamId == TestStreamId).ToListAsync();
        storedEvents.Count.ShouldBe(2);
        storedEvents[0].Version.ShouldBe(1);
        storedEvents[1].Version.ShouldBe(2);
        storedEvents[0].EventType.ShouldContain("PortfolioCreatedEvent");
        storedEvents[1].EventType.ShouldContain("PositionAddedEvent");
    }

    [Fact]
    public async Task AppendEventsAsync_WithIncorrectVersion_ThrowsConcurrencyException()
    {
        // Arrange
        var firstEvent = new PortfolioCreatedEvent(1, "Test Portfolio", 10000m);
        await _eventStore.AppendEventsAsync(TestStreamId, new[] { firstEvent }, expectedVersion: 0);

        var secondEvent = new PositionAddedEvent(1, "AAPL", 10, 150m, DateTime.UtcNow);

        // Act & Assert
        var exception = await Should.ThrowAsync<ConcurrencyException>(async () =>
            await _eventStore.AppendEventsAsync(TestStreamId, new[] { secondEvent }, expectedVersion: 0)
        );

        exception.StreamId.ShouldBe(TestStreamId);
        exception.ExpectedVersion.ShouldBe(0);
        exception.ActualVersion.ShouldBe(1);
    }

    [Fact]
    public async Task AppendEventsAsync_WithEmptyEventList_DoesNothing()
    {
        // Arrange
        var emptyEvents = new List<DomainEvent>();

        // Act
        await _eventStore.AppendEventsAsync(TestStreamId, emptyEvents, expectedVersion: 0);

        // Assert
        var storedEvents = await _context.Events.Where(e => e.StreamId == TestStreamId).ToListAsync();
        storedEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task AppendEventsAsync_WithNullStreamId_ThrowsArgumentException()
    {
        // Arrange
        var events = new[] { new PortfolioCreatedEvent(1, "Test", 10000m) };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _eventStore.AppendEventsAsync(null!, events, expectedVersion: 0)
        );
    }

    [Fact]
    public async Task AppendEventsAsync_WithNullEvents_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _eventStore.AppendEventsAsync(TestStreamId, null!, expectedVersion: 0)
        );
    }

    #endregion

    #region GetEventsAsync Tests

    [Fact]
    public async Task GetEventsAsync_ReturnsEventsInOrder()
    {
        // Arrange
        var events = new List<DomainEvent>
        {
            new PortfolioCreatedEvent(1, "Test Portfolio", 10000m),
            new PositionAddedEvent(1, "AAPL", 10, 150m, DateTime.UtcNow),
            new PositionAddedEvent(1, "MSFT", 5, 300m, DateTime.UtcNow)
        };
        await _eventStore.AppendEventsAsync(TestStreamId, events, expectedVersion: 0);

        // Act
        var retrievedEvents = await _eventStore.GetEventsAsync(TestStreamId);

        // Assert
        retrievedEvents.Count.ShouldBe(3);
        retrievedEvents[0].ShouldBeOfType<PortfolioCreatedEvent>();
        retrievedEvents[1].ShouldBeOfType<PositionAddedEvent>();
        retrievedEvents[2].ShouldBeOfType<PositionAddedEvent>();

        var firstPosition = (PositionAddedEvent)retrievedEvents[1];
        firstPosition.Ticker.ShouldBe("AAPL");

        var secondPosition = (PositionAddedEvent)retrievedEvents[2];
        secondPosition.Ticker.ShouldBe("MSFT");
    }

    [Fact]
    public async Task GetEventsAsync_WithFromVersion_ReturnsOnlyNewEvents()
    {
        // Arrange
        var events = new List<DomainEvent>
        {
            new PortfolioCreatedEvent(1, "Test Portfolio", 10000m),
            new PositionAddedEvent(1, "AAPL", 10, 150m, DateTime.UtcNow),
            new PositionAddedEvent(1, "MSFT", 5, 300m, DateTime.UtcNow)
        };
        await _eventStore.AppendEventsAsync(TestStreamId, events, expectedVersion: 0);

        // Act - Get events from version 1 onwards
        var retrievedEvents = await _eventStore.GetEventsAsync(TestStreamId, fromVersion: 1);

        // Assert
        retrievedEvents.Count.ShouldBe(2);
        retrievedEvents[0].ShouldBeOfType<PositionAddedEvent>();
        retrievedEvents[1].ShouldBeOfType<PositionAddedEvent>();
    }

    [Fact]
    public async Task GetEventsAsync_ForNonExistentStream_ReturnsEmptyList()
    {
        // Act
        var events = await _eventStore.GetEventsAsync("non-existent-stream");

        // Assert
        events.ShouldBeEmpty();
    }

    #endregion

    #region GetStreamVersionAsync Tests

    [Fact]
    public async Task GetStreamVersionAsync_ForNewStream_ReturnsZero()
    {
        // Act
        int version = await _eventStore.GetStreamVersionAsync("new-stream");

        // Assert
        version.ShouldBe(0);
    }

    [Fact]
    public async Task GetStreamVersionAsync_AfterAppend_ReturnsCorrectVersion()
    {
        // Arrange
        var events = new List<DomainEvent>
        {
            new PortfolioCreatedEvent(1, "Test Portfolio", 10000m),
            new PositionAddedEvent(1, "AAPL", 10, 150m, DateTime.UtcNow)
        };
        await _eventStore.AppendEventsAsync(TestStreamId, events, expectedVersion: 0);

        // Act
        int version = await _eventStore.GetStreamVersionAsync(TestStreamId);

        // Assert
        version.ShouldBe(2);
    }

    #endregion

    #region StreamExistsAsync Tests

    [Fact]
    public async Task StreamExistsAsync_ForExistingStream_ReturnsTrue()
    {
        // Arrange
        var events = new[] { new PortfolioCreatedEvent(1, "Test Portfolio", 10000m) };
        await _eventStore.AppendEventsAsync(TestStreamId, events, expectedVersion: 0);

        // Act
        bool exists = await _eventStore.StreamExistsAsync(TestStreamId);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task StreamExistsAsync_ForNonExistentStream_ReturnsFalse()
    {
        // Act
        bool exists = await _eventStore.StreamExistsAsync("non-existent-stream");

        // Assert
        exists.ShouldBeFalse();
    }

    #endregion
}
