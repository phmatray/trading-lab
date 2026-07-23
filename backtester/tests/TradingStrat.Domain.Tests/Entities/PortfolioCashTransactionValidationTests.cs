using Shouldly;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Tests.Entities;

public class PortfolioCashTransactionValidationTests
{
    #region Amount Validation

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(1000)]
    public void CashTransaction_WithPositiveAmount_ShouldBeCreated(decimal amount)
    {
        // Arrange & Act
        PortfolioCashTransaction transaction = new()
        {
            PortfolioId = 1,
            Type = TransactionType.Deposit,
            Amount = amount,
            TransactionDate = DateTime.Today
        };

        // Assert
        transaction.Amount.ShouldBe(amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void CashTransaction_WithInvalidAmount_ShouldThrow(decimal amount)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new PortfolioCashTransaction
        {
            PortfolioId = 1,
            Type = TransactionType.Deposit,
            Amount = amount,
            TransactionDate = DateTime.Today
        });
    }

    #endregion

    #region TransactionDate Validation

    [Fact]
    public void CashTransaction_WithFutureDate_ShouldThrow()
    {
        // Arrange
        DateTime futureDate = DateTime.Today.AddDays(1);

        // Act & Assert
        Should.Throw<ArgumentException>(() => new PortfolioCashTransaction
        {
            PortfolioId = 1,
            Type = TransactionType.Deposit,
            Amount = 1000m,
            TransactionDate = futureDate
        });
    }

    [Fact]
    public void CashTransaction_WithTodayDate_ShouldBeCreated()
    {
        // Arrange & Act
        PortfolioCashTransaction transaction = new()
        {
            PortfolioId = 1,
            Type = TransactionType.Deposit,
            Amount = 1000m,
            TransactionDate = DateTime.Today
        };

        // Assert
        transaction.TransactionDate.ShouldBe(DateTime.Today);
    }

    [Fact]
    public void CashTransaction_WithPastDate_ShouldBeCreated()
    {
        // Arrange
        DateTime pastDate = DateTime.Today.AddDays(-30);

        // Act
        PortfolioCashTransaction transaction = new()
        {
            PortfolioId = 1,
            Type = TransactionType.Deposit,
            Amount = 1000m,
            TransactionDate = pastDate
        };

        // Assert
        transaction.TransactionDate.ShouldBe(pastDate);
    }

    #endregion

    #region Type Validation

    [Theory]
    [InlineData(TransactionType.Deposit)]
    [InlineData(TransactionType.Withdrawal)]
    public void CashTransaction_WithValidType_ShouldBeCreated(TransactionType type)
    {
        // Arrange & Act
        PortfolioCashTransaction transaction = new()
        {
            PortfolioId = 1,
            Type = type,
            Amount = 1000m,
            TransactionDate = DateTime.Today
        };

        // Assert
        transaction.Type.ShouldBe(type);
    }

    #endregion

    #region PortfolioId Validation

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CashTransaction_WithInvalidPortfolioId_ShouldThrow(int portfolioId)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new PortfolioCashTransaction
        {
            PortfolioId = portfolioId,
            Type = TransactionType.Deposit,
            Amount = 1000m,
            TransactionDate = DateTime.Today
        });
    }

    #endregion
}
