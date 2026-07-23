using Shouldly;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Events;
using TradingStrat.Domain.Exceptions;

namespace TradingStrat.Domain.Tests.Entities;

/// <summary>
/// Tests for Portfolio event sourcing capabilities.
/// Verifies event generation, event replay, and state reconstruction from events.
/// </summary>
public class PortfolioEventSourcingTests
{
    #region Event Generation Tests

    [Fact]
    public void AddPosition_RaisesPositionAddedEvent()
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

        // Act
        portfolio.AddPosition(position);

        // Assert
        IReadOnlyList<DomainEvent> events = portfolio.GetDomainEvents();
        events.Count.ShouldBe(1);

        PositionAddedEvent positionEvent = events[0].ShouldBeOfType<PositionAddedEvent>();
        positionEvent.PortfolioId.ShouldBe(1);
        positionEvent.Ticker.ShouldBe("AAPL");
        positionEvent.Quantity.ShouldBe(10);
        positionEvent.EntryPrice.ShouldBe(150m);
    }

    [Fact]
    public void RemovePosition_RaisesPositionRemovedEvent()
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
        portfolio.ClearDomainEvents(); // Clear the AddPosition event before testing RemovePosition

        // Act
        portfolio.RemovePosition("AAPL");

        // Assert
        IReadOnlyList<DomainEvent> events = portfolio.GetDomainEvents();
        events.Count.ShouldBe(1);

        PositionRemovedEvent removedEvent = events[0].ShouldBeOfType<PositionRemovedEvent>();
        removedEvent.Ticker.ShouldBe("AAPL");
    }

    [Fact]
    public void UpdatePositionQuantity_RaisesPositionQuantityChangedEvent()
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
        portfolio.ClearDomainEvents(); // Clear the AddPosition event

        // Act
        portfolio.UpdatePositionQuantity("AAPL", 15);

        // Assert
        IReadOnlyList<DomainEvent> events = portfolio.GetDomainEvents();
        events.Count.ShouldBe(1);

        PositionQuantityChangedEvent quantityEvent = events[0].ShouldBeOfType<PositionQuantityChangedEvent>();
        quantityEvent.Ticker.ShouldBe("AAPL");
        quantityEvent.OldQuantity.ShouldBe(10);
        quantityEvent.NewQuantity.ShouldBe(15);
    }

    [Fact]
    public void RecordCashTransaction_Deposit_RaisesCashTransactionRecordedEvent()
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

        // Act
        portfolio.RecordCashTransaction(
            TransactionType.Deposit,
            5000m,
            DateTime.UtcNow);

        // Assert
        IReadOnlyList<DomainEvent> events = portfolio.GetDomainEvents();
        events.Count.ShouldBe(1);

        CashTransactionRecordedEvent cashEvent = events[0].ShouldBeOfType<CashTransactionRecordedEvent>();
        cashEvent.Type.ShouldBe(TransactionType.Deposit);
        cashEvent.Amount.ShouldBe(5000m);

        portfolio.Cash.ShouldBe(15000m);
    }

    [Fact]
    public void MultipleOperations_GeneratesMultipleEventsInOrder()
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

        // Act - Perform multiple operations
        var position1 = new Position
        {
            Id = 1,
            PortfolioId = 1,
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 150m,
            EntryDate = DateTime.Today.AddDays(-1)
        };
        portfolio.AddPosition(position1);

        var position2 = new Position
        {
            Id = 2,
            PortfolioId = 1,
            Ticker = "MSFT",
            Quantity = 5,
            EntryPrice = 300m,
            EntryDate = DateTime.Today.AddDays(-1)
        };
        portfolio.AddPosition(position2);

        portfolio.RecordCashTransaction(TransactionType.Deposit, 5000m, DateTime.UtcNow);

        // Assert
        IReadOnlyList<DomainEvent> events = portfolio.GetDomainEvents();
        events.Count.ShouldBe(3);

        events[0].ShouldBeOfType<PositionAddedEvent>();
        ((PositionAddedEvent)events[0]).Ticker.ShouldBe("AAPL");

        events[1].ShouldBeOfType<PositionAddedEvent>();
        ((PositionAddedEvent)events[1]).Ticker.ShouldBe("MSFT");

        events[2].ShouldBeOfType<CashTransactionRecordedEvent>();
    }

    #endregion

    #region Event Replay Tests

    [Fact]
    public void LoadFromHistory_PortfolioCreatedEvent_RebuildsState()
    {
        // Arrange
        var portfolio = new Portfolio();
        var events = new List<DomainEvent>
        {
            new PortfolioCreatedEvent(1, "Test Portfolio", 10000m)
        };

        // Act
        portfolio.LoadFromHistory(events);

        // Assert
        portfolio.Id.ShouldBe(1);
        portfolio.Name.ShouldBe("Test Portfolio");
        portfolio.Cash.ShouldBe(10000m);
        portfolio.Version.ShouldBe(1);
    }

    [Fact]
    public void LoadFromHistory_PositionAddedEvent_RebuildsPositions()
    {
        // Arrange
        var portfolio = new Portfolio();
        var events = new List<DomainEvent>
        {
            new PortfolioCreatedEvent(1, "Test Portfolio", 10000m),
            new PositionAddedEvent(1, "AAPL", 10, 150m, DateTime.Today.AddDays(-1)),
            new PositionAddedEvent(1, "MSFT", 5, 300m, DateTime.Today.AddDays(-1))
        };

        // Act
        portfolio.LoadFromHistory(events);

        // Assert
        portfolio.Version.ShouldBe(3);
        portfolio.Positions.Count.ShouldBe(2);
        portfolio.Positions[0].Ticker.ShouldBe("AAPL");
        portfolio.Positions[0].Quantity.ShouldBe(10);
        portfolio.Positions[1].Ticker.ShouldBe("MSFT");
        portfolio.Positions[1].Quantity.ShouldBe(5);
    }

    [Fact]
    public void LoadFromHistory_PositionRemovedEvent_RemovesPosition()
    {
        // Arrange
        var portfolio = new Portfolio();
        var events = new List<DomainEvent>
        {
            new PortfolioCreatedEvent(1, "Test Portfolio", 10000m),
            new PositionAddedEvent(1, "AAPL", 10, 150m, DateTime.Today.AddDays(-1)),
            new PositionAddedEvent(1, "MSFT", 5, 300m, DateTime.Today.AddDays(-1)),
            new PositionRemovedEvent(1, "AAPL", 10)
        };

        // Act
        portfolio.LoadFromHistory(events);

        // Assert
        portfolio.Version.ShouldBe(4);
        portfolio.Positions.Count.ShouldBe(1);
        portfolio.Positions[0].Ticker.ShouldBe("MSFT");
    }

    [Fact]
    public void LoadFromHistory_PositionQuantityChangedEvent_UpdatesQuantity()
    {
        // Arrange
        var portfolio = new Portfolio();
        var events = new List<DomainEvent>
        {
            new PortfolioCreatedEvent(1, "Test Portfolio", 10000m),
            new PositionAddedEvent(1, "AAPL", 10, 150m, DateTime.Today.AddDays(-1)),
            new PositionQuantityChangedEvent(1, "AAPL", 10, 15)
        };

        // Act
        portfolio.LoadFromHistory(events);

        // Assert
        portfolio.Version.ShouldBe(3);
        portfolio.Positions.Count.ShouldBe(1);
        portfolio.Positions[0].Quantity.ShouldBe(15);
    }

    [Fact]
    public void LoadFromHistory_CashTransactionRecordedEvent_UpdatesCash()
    {
        // Arrange
        var portfolio = new Portfolio();
        var events = new List<DomainEvent>
        {
            new PortfolioCreatedEvent(1, "Test Portfolio", 10000m),
            new CashTransactionRecordedEvent(1, TransactionType.Deposit, 5000m, DateTime.UtcNow),
            new CashTransactionRecordedEvent(1, TransactionType.Withdrawal, 2000m, DateTime.UtcNow)
        };

        // Act
        portfolio.LoadFromHistory(events);

        // Assert
        portfolio.Version.ShouldBe(3);
        portfolio.Cash.ShouldBe(13000m); // 10000 + 5000 - 2000
    }

    [Fact]
    public void LoadFromHistory_ComplexScenario_RebuildsCorrectState()
    {
        // Arrange
        var portfolio = new Portfolio();
        var events = new List<DomainEvent>
        {
            new PortfolioCreatedEvent(1, "Test Portfolio", 10000m),
            new PositionAddedEvent(1, "AAPL", 10, 150m, DateTime.Today.AddDays(-1)),
            new PositionAddedEvent(1, "MSFT", 5, 300m, DateTime.Today.AddDays(-1)),
            new CashTransactionRecordedEvent(1, TransactionType.Deposit, 5000m, DateTime.UtcNow),
            new PositionQuantityChangedEvent(1, "AAPL", 10, 15),
            new PositionRemovedEvent(1, "MSFT", 5),
            new CashTransactionRecordedEvent(1, TransactionType.Withdrawal, 2000m, DateTime.UtcNow)
        };

        // Act
        portfolio.LoadFromHistory(events);

        // Assert
        portfolio.Version.ShouldBe(7);
        portfolio.Id.ShouldBe(1);
        portfolio.Name.ShouldBe("Test Portfolio");
        portfolio.Cash.ShouldBe(13000m); // 10000 + 5000 - 2000
        portfolio.Positions.Count.ShouldBe(1);
        portfolio.Positions[0].Ticker.ShouldBe("AAPL");
        portfolio.Positions[0].Quantity.ShouldBe(15);
    }

    #endregion

    #region Version Tracking Tests

    [Fact]
    public void Version_IncrementsWithEachEvent()
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

        portfolio.Version.ShouldBe(0);

        // Act & Assert
        var position1 = new Position
        {
            Id = 1,
            PortfolioId = 1,
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 150m,
            EntryDate = DateTime.Today.AddDays(-1)
        };
        portfolio.AddPosition(position1);
        portfolio.Version.ShouldBe(1);

        var position2 = new Position
        {
            Id = 2,
            PortfolioId = 1,
            Ticker = "MSFT",
            Quantity = 5,
            EntryPrice = 300m,
            EntryDate = DateTime.Today.AddDays(-1)
        };
        portfolio.AddPosition(position2);
        portfolio.Version.ShouldBe(2);

        portfolio.RecordCashTransaction(TransactionType.Deposit, 5000m, DateTime.UtcNow);
        portfolio.Version.ShouldBe(3);
    }

    [Fact]
    public void LoadFromHistory_SetsCorrectVersion()
    {
        // Arrange
        var portfolio = new Portfolio();
        var events = new List<DomainEvent>
        {
            new PortfolioCreatedEvent(1, "Test Portfolio", 10000m),
            new PositionAddedEvent(1, "AAPL", 10, 150m, DateTime.Today.AddDays(-1)),
            new PositionAddedEvent(1, "MSFT", 5, 300m, DateTime.Today.AddDays(-1))
        };

        // Act
        portfolio.LoadFromHistory(events);

        // Assert
        portfolio.Version.ShouldBe(3);
    }

    #endregion

    #region Invariant Enforcement Tests

    [Fact]
    public void AddPosition_WithDuplicateTicker_ThrowsDuplicatePositionException()
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
        portfolio.ClearDomainEvents(); // Clear the AddPosition event before testing duplicate behavior

        var duplicatePosition = new Position
        {
            Id = 2,
            PortfolioId = 1,
            Ticker = "AAPL",
            Quantity = 5,
            EntryPrice = 160m,
            EntryDate = DateTime.Today.AddDays(-1)
        };

        // Act & Assert
        DuplicatePositionException exception = Should.Throw<DuplicatePositionException>(() =>
            portfolio.AddPosition(duplicatePosition));

        exception.Ticker.ShouldBe("AAPL");

        // No event should be raised for the failed duplicate addition
        portfolio.GetDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public void RemovePosition_NonExistentTicker_ThrowsPositionNotFoundException()
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

        // Act & Assert
        PositionNotFoundException exception = Should.Throw<PositionNotFoundException>(() =>
            portfolio.RemovePosition("AAPL"));

        exception.Ticker.ShouldBe("AAPL");
    }

    [Fact]
    public void RecordCashTransaction_Withdrawal_InsufficientCash_ThrowsInsufficientCashException()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 1,
            Name = "Test Portfolio",
            Cash = 1000m,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        // Act & Assert
        InsufficientCashException exception = Should.Throw<InsufficientCashException>(() =>
            portfolio.RecordCashTransaction(TransactionType.Withdrawal, 2000m, DateTime.UtcNow));

        exception.AvailableCash.ShouldBe(1000m);
        exception.RequiredAmount.ShouldBe(2000m);
    }

    #endregion
}
