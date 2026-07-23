using FakeItEasy;
using FakeItEasy.Core;
using Shouldly;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Events;

namespace TradingStrat.Infrastructure.Tests.Persistence.EventStore;

/// <summary>
/// Tests for AggregateRepository implementation.
/// Verifies aggregate loading from snapshots + events, saving, and snapshot optimization.
/// </summary>
public class AggregateRepositoryTests
{
    private readonly IEventStore _fakeEventStore;
    private readonly ISnapshotStore _fakeSnapshotStore;
    private readonly AggregateRepository<Portfolio> _repository;

    public AggregateRepositoryTests()
    {
        _fakeEventStore = A.Fake<IEventStore>();
        _fakeSnapshotStore = A.Fake<ISnapshotStore>();
        _repository = new AggregateRepository<Portfolio>(_fakeEventStore, _fakeSnapshotStore);
    }

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_FromEmptyStream_ReturnsNull()
    {
        // Arrange
        A.CallTo(() => _fakeSnapshotStore.GetSnapshotAsync<Portfolio>("1"))
            .Returns(Task.FromResult<AggregateSnapshot<Portfolio>?>(null));

        A.CallTo(() => _fakeEventStore.GetEventsAsync("1", 0))
            .Returns(Task.FromResult(new List<DomainEvent>()));

        // Act
        Portfolio? portfolio = await _repository.LoadAsync("1");

        // Assert
        portfolio.ShouldBeNull();
    }

    [Fact]
    public async Task LoadAsync_ReplaysEventsInOrder()
    {
        // Arrange
        var events = new List<DomainEvent>
        {
            new PortfolioCreatedEvent(1, "Test Portfolio", 10000m),
            new PositionAddedEvent(1, "AAPL", 10, 150m, DateTime.Today.AddDays(-1)),
            new PositionAddedEvent(1, "MSFT", 5, 300m, DateTime.Today.AddDays(-1))
        };

        A.CallTo(() => _fakeSnapshotStore.GetSnapshotAsync<Portfolio>("1"))
            .Returns(Task.FromResult<AggregateSnapshot<Portfolio>?>(null));

        A.CallTo(() => _fakeEventStore.GetEventsAsync("1", 0))
            .Returns(Task.FromResult(events));

        // Act
        Portfolio? portfolio = await _repository.LoadAsync("1");

        // Assert
        portfolio.ShouldNotBeNull();
        portfolio.Id.ShouldBe(1);
        portfolio.Name.ShouldBe("Test Portfolio");
        portfolio.Cash.ShouldBe(10000m);
        portfolio.Version.ShouldBe(3);
        portfolio.Positions.Count.ShouldBe(2);
        portfolio.Positions[0].Ticker.ShouldBe("AAPL");
        portfolio.Positions[1].Ticker.ShouldBe("MSFT");
    }

    [Fact]
    public async Task LoadAsync_FromSnapshot_LoadsOnlyNewEvents()
    {
        // Arrange - Snapshot at version 2
        var snapshotPortfolio = new Portfolio
        {
            Id = 1,
            Name = "Test Portfolio",
            Cash = 10000m,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        var position = new Position
        {
            Id = 1,
            PortfolioId = 1,
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 150m,
            EntryDate = DateTime.Today.AddDays(-1)
        };

        snapshotPortfolio.AddPosition(position);

        typeof(Portfolio).GetProperty("Version")!.SetValue(snapshotPortfolio, 2);

        var snapshot = new AggregateSnapshot<Portfolio>
        {
            Aggregate = snapshotPortfolio,
            Version = 2,
            CreatedAt = DateTime.UtcNow
        };

        // Events after snapshot
        var eventsAfterSnapshot = new List<DomainEvent>
        {
            new PositionAddedEvent(1, "MSFT", 5, 300m, DateTime.Today.AddDays(-1))
        };

        A.CallTo(() => _fakeSnapshotStore.GetSnapshotAsync<Portfolio>("1"))
            .Returns(Task.FromResult<AggregateSnapshot<Portfolio>?>(snapshot));

        A.CallTo(() => _fakeEventStore.GetEventsAsync("1", 2))
            .Returns(Task.FromResult(eventsAfterSnapshot));

        // Act
        Portfolio? portfolio = await _repository.LoadAsync("1");

        // Assert
        portfolio.ShouldNotBeNull();
        portfolio.Version.ShouldBe(3); // Snapshot version 2 + 1 new event
        portfolio.Positions.Count.ShouldBe(2);
        portfolio.Positions[0].Ticker.ShouldBe("AAPL");
        portfolio.Positions[1].Ticker.ShouldBe("MSFT");

        // Verify only events after snapshot were loaded
        A.CallTo(() => _fakeEventStore.GetEventsAsync("1", 2))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task LoadAsync_ClearsUncommittedEvents()
    {
        // Arrange
        var events = new List<DomainEvent>
        {
            new PortfolioCreatedEvent(1, "Test Portfolio", 10000m)
        };

        A.CallTo(() => _fakeSnapshotStore.GetSnapshotAsync<Portfolio>("1"))
            .Returns(Task.FromResult<AggregateSnapshot<Portfolio>?>(null));

        A.CallTo(() => _fakeEventStore.GetEventsAsync("1", 0))
            .Returns(Task.FromResult(events));

        // Act
        Portfolio? portfolio = await _repository.LoadAsync("1");

        // Assert - Uncommitted events should be cleared after reconstruction
        portfolio.ShouldNotBeNull();
        portfolio.GetDomainEvents().ShouldBeEmpty();
    }

    #endregion

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_WithNoChanges_DoesNothing()
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
        // No domain events raised

        // Act
        await _repository.SaveAsync(portfolio);

        // Assert - No calls to event store
        A.CallTo(() => _fakeEventStore.AppendEventsAsync(A<string>._, A<IEnumerable<DomainEvent>>._, A<int>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task SaveAsync_AppendsUncommittedEvents()
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

        // Raise domain event
        var position = new Position
        {
            Id = 1,
            PortfolioId = 1,
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 150m,
            EntryDate = DateTime.Today.AddDays(-1)
        };
        portfolio.AddPosition(position);

        // Verify there's 1 uncommitted event before saving
        portfolio.GetDomainEvents().Count.ShouldBe(1);
        portfolio.GetDomainEvents()[0].ShouldBeOfType<PositionAddedEvent>();

        // Act
        await _repository.SaveAsync(portfolio);

        // Assert - Capture what was actually called
        List<ICompletedFakeObjectCall> calls = Fake.GetCalls(_fakeEventStore).ToList();
        calls.Count.ShouldBe(1);

        ICompletedFakeObjectCall call = calls[0];
        call.Method.Name.ShouldBe("AppendEventsAsync");

        string? streamId = call.Arguments[0] as string;
        IEnumerable<DomainEvent>? eventsEnumerable = call.Arguments[1] as IEnumerable<DomainEvent>;
        List<DomainEvent> events = eventsEnumerable?.ToList() ?? new List<DomainEvent>();
        object? expectedVersion = call.Arguments[2];

        streamId.ShouldBe("1");
        events.ShouldNotBeNull();
        events.Count.ShouldBe(1);
        events.First().ShouldBeOfType<PositionAddedEvent>();
        expectedVersion.ShouldBe(0);
    }

    [Fact]
    public async Task SaveAsync_ClearsUncommittedEventsAfterSave()
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

        var position = new Position
        {
            Id = 1,
            PortfolioId = 1,
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 150m,
            EntryDate = DateTime.Today.AddDays(-1)
        };
        portfolio.AddPosition(position);

        portfolio.GetDomainEvents().Count.ShouldBe(1);

        // Act
        await _repository.SaveAsync(portfolio);

        // Assert
        portfolio.GetDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveAsync_AtSnapshotInterval_CreatesSnapshot()
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

        // Manually set version to 49 (next save will be version 50, snapshot interval)
        typeof(Portfolio).GetProperty("Version")!.SetValue(portfolio, 49);

        var position = new Position
        {
            Id = 1,
            PortfolioId = 1,
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 150m,
            EntryDate = DateTime.Today.AddDays(-1)
        };
        portfolio.AddPosition(position);

        // Version is now 50 after AddPosition

        // Act
        await _repository.SaveAsync(portfolio);

        // Assert
        A.CallTo(() => _fakeSnapshotStore.SaveSnapshotAsync(portfolio))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SaveAsync_NotAtSnapshotInterval_DoesNotCreateSnapshot()
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

        // Version will be 1 after adding position
        var position = new Position
        {
            Id = 1,
            PortfolioId = 1,
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 150m,
            EntryDate = DateTime.Today.AddDays(-1)
        };
        portfolio.AddPosition(position);

        // Act
        await _repository.SaveAsync(portfolio);

        // Assert
        A.CallTo(() => _fakeSnapshotStore.SaveSnapshotAsync(A<Portfolio>._))
            .MustNotHaveHappened();
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_ForExistingStream_ReturnsTrue()
    {
        // Arrange
        A.CallTo(() => _fakeEventStore.StreamExistsAsync("1"))
            .Returns(Task.FromResult(true));

        // Act
        bool exists = await _repository.ExistsAsync("1");

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ForNonExistentStream_ReturnsFalse()
    {
        // Arrange
        A.CallTo(() => _fakeEventStore.StreamExistsAsync("non-existent"))
            .Returns(Task.FromResult(false));

        // Act
        bool exists = await _repository.ExistsAsync("non-existent");

        // Assert
        exists.ShouldBeFalse();
    }

    #endregion

    #region GetVersionAsync Tests

    [Fact]
    public async Task GetVersionAsync_ReturnsStreamVersion()
    {
        // Arrange
        A.CallTo(() => _fakeEventStore.GetStreamVersionAsync("1"))
            .Returns(Task.FromResult(42));

        // Act
        int version = await _repository.GetVersionAsync("1");

        // Assert
        version.ShouldBe(42);
    }

    #endregion
}
