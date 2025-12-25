using Shouldly;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Events;
using TradingStrat.Domain.Exceptions;

namespace TradingStrat.Domain.Tests.Entities;

public class PortfolioAggregateTests
{
    #region AddPosition Tests

    [Fact]
    public void AddPosition_WithValidPosition_AddsToCollection()
    {
        // Arrange
        var portfolio = new Portfolio { Id = 1, Name = "Test", Cash = 10000m };
        var position = new Position { Ticker = "AAPL", Quantity = 10, EntryPrice = 150m, EntryDate = DateTime.Today };

        // Act
        portfolio.AddPosition(position);

        // Assert
        portfolio.Positions.Count.ShouldBe(1);
        portfolio.Positions[0].Ticker.ShouldBe("AAPL");
    }

    [Fact]
    public void AddPosition_WithValidPosition_RaisesPositionAddedEvent()
    {
        // Arrange
        var portfolio = new Portfolio { Id = 1, Name = "Test", Cash = 10000m };
        var position = new Position { Ticker = "AAPL", Quantity = 10, EntryPrice = 150m, EntryDate = DateTime.Today };

        // Act
        portfolio.AddPosition(position);

        // Assert
        var events = portfolio.GetDomainEvents();
        events.Count.ShouldBe(1);
        var positionEvent = events[0].ShouldBeOfType<PositionAddedEvent>();
        positionEvent.Ticker.ShouldBe("AAPL");
        positionEvent.Quantity.ShouldBe(10);
        positionEvent.EntryPrice.ShouldBe(150m);
    }

    [Fact]
    public void AddPosition_WhenTickerAlreadyExists_ThrowsDuplicatePositionException()
    {
        // Arrange
        var portfolio = new Portfolio { Id = 1, Name = "Test", Cash = 10000m };
        portfolio.AddPosition(new Position { Ticker = "AAPL", Quantity = 10, EntryPrice = 150m, EntryDate = DateTime.Today });

        // Act & Assert
        var exception = Should.Throw<DuplicatePositionException>(() =>
            portfolio.AddPosition(new Position { Ticker = "AAPL", Quantity = 5, EntryPrice = 155m, EntryDate = DateTime.Today })
        );
        exception.Ticker.ShouldBe("AAPL");
    }

    [Fact]
    public void AddPosition_WithNullPosition_ThrowsArgumentNullException()
    {
        // Arrange
        var portfolio = new Portfolio { Id = 1, Name = "Test", Cash = 10000m };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => portfolio.AddPosition(null!));
    }

    #endregion

    #region RemovePosition Tests

    [Fact]
    public void RemovePosition_WithExistingTicker_RemovesFromCollection()
    {
        // Arrange
        var portfolio = new Portfolio { Id = 1, Name = "Test", Cash = 10000m };
        portfolio.AddPosition(new Position { Ticker = "AAPL", Quantity = 10, EntryPrice = 150m, EntryDate = DateTime.Today });
        portfolio.ClearDomainEvents();

        // Act
        portfolio.RemovePosition("AAPL");

        // Assert
        portfolio.Positions.ShouldBeEmpty();
    }

    [Fact]
    public void RemovePosition_WithExistingTicker_RaisesPositionRemovedEvent()
    {
        // Arrange
        var portfolio = new Portfolio { Id = 1, Name = "Test", Cash = 10000m };
        portfolio.AddPosition(new Position { Ticker = "AAPL", Quantity = 10, EntryPrice = 150m, EntryDate = DateTime.Today });
        portfolio.ClearDomainEvents();

        // Act
        portfolio.RemovePosition("AAPL");

        // Assert
        var events = portfolio.GetDomainEvents();
        events.Count.ShouldBe(1);
        var removedEvent = events[0].ShouldBeOfType<PositionRemovedEvent>();
        removedEvent.Ticker.ShouldBe("AAPL");
        removedEvent.Quantity.ShouldBe(10);
    }

    [Fact]
    public void RemovePosition_WithNonExistentTicker_ThrowsPositionNotFoundException()
    {
        // Arrange
        var portfolio = new Portfolio { Id = 1, Name = "Test", Cash = 10000m };

        // Act & Assert
        var exception = Should.Throw<PositionNotFoundException>(() =>
            portfolio.RemovePosition("MSFT")
        );
        exception.Ticker.ShouldBe("MSFT");
    }

    #endregion

    #region RecordCashTransaction Tests

    [Fact]
    public void RecordCashTransaction_WithDeposit_IncreasesCash()
    {
        // Arrange
        var portfolio = new Portfolio { Id = 1, Name = "Test", Cash = 10000m };

        // Act
        portfolio.RecordCashTransaction(TransactionType.Deposit, 5000m, DateTime.Today);

        // Assert
        portfolio.Cash.ShouldBe(15000m);
    }

    [Fact]
    public void RecordCashTransaction_WithDeposit_RaisesCashTransactionEvent()
    {
        // Arrange
        var portfolio = new Portfolio { Id = 1, Name = "Test", Cash = 10000m };
        var transactionDate = DateTime.Today;

        // Act
        portfolio.RecordCashTransaction(TransactionType.Deposit, 5000m, transactionDate);

        // Assert
        var events = portfolio.GetDomainEvents();
        events.Count.ShouldBe(1);
        var cashEvent = events[0].ShouldBeOfType<CashTransactionRecordedEvent>();
        cashEvent.Type.ShouldBe(TransactionType.Deposit);
        cashEvent.Amount.ShouldBe(5000m);
        cashEvent.TransactionDate.ShouldBe(transactionDate);
    }

    [Fact]
    public void RecordCashTransaction_WithWithdrawal_DecreasesCash()
    {
        // Arrange
        var portfolio = new Portfolio { Id = 1, Name = "Test", Cash = 10000m };

        // Act
        portfolio.RecordCashTransaction(TransactionType.Withdrawal, 3000m, DateTime.Today);

        // Assert
        portfolio.Cash.ShouldBe(7000m);
    }

    [Fact]
    public void RecordCashTransaction_WithExcessiveWithdrawal_ThrowsInsufficientCashException()
    {
        // Arrange
        var portfolio = new Portfolio { Id = 1, Name = "Test", Cash = 10000m };

        // Act & Assert
        var exception = Should.Throw<InsufficientCashException>(() =>
            portfolio.RecordCashTransaction(TransactionType.Withdrawal, 15000m, DateTime.Today)
        );
        exception.AvailableCash.ShouldBe(10000m);
        exception.RequiredAmount.ShouldBe(15000m);
    }

    #endregion

    #region Positions Collection Immutability

    [Fact]
    public void Positions_ReturnsReadOnlyCollection()
    {
        // Arrange
        var portfolio = new Portfolio { Id = 1, Name = "Test", Cash = 10000m };
        portfolio.AddPosition(new Position { Ticker = "AAPL", Quantity = 10, EntryPrice = 150m, EntryDate = DateTime.Today });

        // Act
        var positions = portfolio.Positions;

        // Assert
        positions.ShouldBeAssignableTo<IReadOnlyList<Position>>();
    }

    #endregion
}
