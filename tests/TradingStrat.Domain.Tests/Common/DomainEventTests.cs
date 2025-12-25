using Shouldly;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Events;

namespace TradingStrat.Domain.Tests.Common;

public class DomainEventTests
{
    #region DomainEvent Base Class Tests

    [Fact]
    public void DomainEvent_WhenCreated_CapturesUtcTimestamp()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var @event = new PortfolioCreatedEvent(1, "Test Portfolio", 10000m);

        // Assert
        var afterCreate = DateTime.UtcNow;
        @event.OccurredAt.ShouldBeInRange(beforeCreate, afterCreate);
        @event.OccurredAt.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void DomainEvent_WhenCreated_GeneratesUniqueEventId()
    {
        // Arrange & Act
        var event1 = new PortfolioCreatedEvent(1, "Portfolio 1", 10000m);
        var event2 = new PortfolioCreatedEvent(2, "Portfolio 2", 20000m);

        // Assert
        event1.EventId.ShouldNotBe(Guid.Empty);
        event2.EventId.ShouldNotBe(Guid.Empty);
        event1.EventId.ShouldNotBe(event2.EventId);
    }

    [Fact]
    public void DomainEvent_IsImmutable()
    {
        // Arrange & Act
        var @event = new PortfolioCreatedEvent(1, "Test", 10000m);

        // Assert - verify it's a record (immutable)
        @event.ShouldBeOfType<PortfolioCreatedEvent>();
        @event.EventId.ShouldNotBe(Guid.Empty);
    }

    #endregion

    #region PortfolioCreatedEvent Tests

    [Fact]
    public void PortfolioCreatedEvent_CapturesAllProperties()
    {
        // Arrange & Act
        var @event = new PortfolioCreatedEvent(42, "My Portfolio", 50000m);

        // Assert
        @event.PortfolioId.ShouldBe(42);
        @event.Name.ShouldBe("My Portfolio");
        @event.InitialCash.ShouldBe(50000m);
    }

    #endregion

    #region PositionAddedEvent Tests

    [Fact]
    public void PositionAddedEvent_CapturesAllProperties()
    {
        // Arrange & Act
        var entryDate = DateTime.Today;
        var @event = new PositionAddedEvent(1, "AAPL", 100, 150.50m, entryDate);

        // Assert
        @event.PortfolioId.ShouldBe(1);
        @event.Ticker.ShouldBe("AAPL");
        @event.Quantity.ShouldBe(100);
        @event.EntryPrice.ShouldBe(150.50m);
        @event.EntryDate.ShouldBe(entryDate);
    }

    #endregion

    #region PositionRemovedEvent Tests

    [Fact]
    public void PositionRemovedEvent_CapturesAllProperties()
    {
        // Arrange & Act
        var @event = new PositionRemovedEvent(1, "MSFT", 50);

        // Assert
        @event.PortfolioId.ShouldBe(1);
        @event.Ticker.ShouldBe("MSFT");
        @event.Quantity.ShouldBe(50);
    }

    #endregion

    #region CashTransactionRecordedEvent Tests

    [Fact]
    public void CashTransactionRecordedEvent_CapturesAllProperties()
    {
        // Arrange & Act
        var transactionDate = DateTime.Today;
        var @event = new CashTransactionRecordedEvent(1, TransactionType.Deposit, 5000m, transactionDate);

        // Assert
        @event.PortfolioId.ShouldBe(1);
        @event.Type.ShouldBe(TransactionType.Deposit);
        @event.Amount.ShouldBe(5000m);
        @event.TransactionDate.ShouldBe(transactionDate);
    }

    #endregion
}
