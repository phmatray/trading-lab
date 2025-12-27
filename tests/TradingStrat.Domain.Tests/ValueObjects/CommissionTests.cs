using Shouldly;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.ValueObjects;

public class CommissionTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesInstance()
    {
        // Arrange
        Percentage rate = Percentage.FromPercentage(0.1m);
        Money minimum = new Money(1.0m, "USD");

        // Act
        Commission commission = new Commission(rate, minimum);

        // Assert
        commission.Rate.ShouldBe(rate);
        commission.Minimum.ShouldBe(minimum);
    }

    [Fact]
    public void Constructor_WithNegativeRate_ThrowsArgumentException()
    {
        // Arrange
        Percentage rate = new Percentage(-0.1m);
        Money minimum = new Money(1.0m, "USD");

        // Act & Assert
        Should.Throw<ArgumentException>(() => new Commission(rate, minimum));
    }

    [Fact]
    public void Constructor_WithNegativeMinimum_ThrowsArgumentException()
    {
        // Arrange
        Percentage rate = Percentage.FromPercentage(0.1m);
        Money minimum = new Money(-1.0m, "USD");

        // Act & Assert
        Should.Throw<ArgumentException>(() => new Commission(rate, minimum));
    }

    [Fact]
    public void None_ReturnsZeroCommission()
    {
        // Act
        Commission none = Commission.None;

        // Assert
        none.Rate.Value.ShouldBe(0m);
        none.Minimum.Amount.ShouldBe(0m);
    }

    [Fact]
    public void FromPercentage_CreatesCommissionWithPercentageRate()
    {
        // Act
        Commission commission = Commission.FromPercentage(0.1m, 1.0m, "USD");

        // Assert
        commission.Rate.Value.ShouldBe(0.1m);
        commission.Minimum.Amount.ShouldBe(1.0m);
        commission.Minimum.Currency.ShouldBe("USD");
    }

    [Fact]
    public void FromDecimal_CreatesCommissionWithDecimalRate()
    {
        // Act (0.001 decimal = 0.1%)
        Commission commission = Commission.FromDecimal(0.001m, 1.0m, "USD");

        // Assert
        commission.Rate.Value.ShouldBe(0.1m);
        commission.Minimum.Amount.ShouldBe(1.0m);
    }

    [Fact]
    public void CalculateFor_WhenPercentageExceedsMinimum_ReturnsPercentage()
    {
        // Arrange - 0.1% with $1 minimum
        Commission commission = Commission.FromPercentage(0.1m, 1.0m, "USD");
        Money tradeValue = new Money(10000m, "USD"); // 0.1% of 10000 = $10

        // Act
        Money result = commission.CalculateFor(tradeValue);

        // Assert
        result.Amount.ShouldBe(10m); // $10 (percentage exceeds minimum)
    }

    [Fact]
    public void CalculateFor_WhenPercentageBelowMinimum_ReturnsMinimum()
    {
        // Arrange - 0.1% with $5 minimum
        Commission commission = Commission.FromPercentage(0.1m, 5.0m, "USD");
        Money tradeValue = new Money(1000m, "USD"); // 0.1% of 1000 = $1

        // Act
        Money result = commission.CalculateFor(tradeValue);

        // Assert
        result.Amount.ShouldBe(5m); // $5 (minimum applies)
    }

    [Fact]
    public void CalculateFor_WithZeroRate_ReturnsMinimum()
    {
        // Arrange
        Commission commission = Commission.FromPercentage(0m, 2.0m, "USD");
        Money tradeValue = new Money(1000m, "USD");

        // Act
        Money result = commission.CalculateFor(tradeValue);

        // Assert
        result.Amount.ShouldBe(2m); // Minimum applies
    }

    [Fact]
    public void CalculateFor_WithZeroMinimum_ReturnsPercentage()
    {
        // Arrange
        Commission commission = Commission.FromPercentage(0.5m, 0m, "USD");
        Money tradeValue = new Money(1000m, "USD"); // 0.5% of 1000 = $5

        // Act
        Money result = commission.CalculateFor(tradeValue);

        // Assert
        result.Amount.ShouldBe(5m); // Percentage commission
    }

    [Fact]
    public void CalculateFor_WithDifferentCurrency_ThrowsInvalidOperationException()
    {
        // Arrange
        Commission commission = Commission.FromPercentage(0.1m, 1.0m, "USD");
        Money tradeValue = new Money(1000m, "EUR");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => commission.CalculateFor(tradeValue));
    }

    [Fact]
    public void ToString_WithZeroCommission_ReturnsNoneMessage()
    {
        // Arrange
        Commission commission = Commission.None;

        // Act
        string result = commission.ToString();

        // Assert
        result.ShouldBe("No commission");
    }

    [Fact]
    public void ToString_WithOnlyRate_ReturnsRateOnly()
    {
        // Arrange
        Commission commission = Commission.FromPercentage(0.1m, 0m, "USD");

        // Act
        string result = commission.ToString();

        // Assert
        result.ShouldContain("0.10%");
    }

    [Fact]
    public void ToString_WithOnlyMinimum_ReturnsMinimumOnly()
    {
        // Arrange
        Commission commission = Commission.FromPercentage(0m, 5.0m, "USD");

        // Act
        string result = commission.ToString();

        // Assert
        result.ShouldContain("Min:");
        result.ShouldContain("5.00 USD");
    }

    [Fact]
    public void ToString_WithBothRateAndMinimum_ReturnsBoth()
    {
        // Arrange
        Commission commission = Commission.FromPercentage(0.1m, 1.0m, "USD");

        // Act
        string result = commission.ToString();

        // Assert
        result.ShouldContain("0.10%");
        result.ShouldContain("Min:");
        result.ShouldContain("1.00 USD");
    }

    [Fact]
    public void CalculateFor_RealWorldScenario_InteractiveBrokers()
    {
        // Arrange - Interactive Brokers US stocks: 0.005 per share, $1 minimum
        // For simplicity, treating as percentage of trade value
        Commission commission = Commission.FromPercentage(0.1m, 1.0m, "USD");

        // Small trade: $500 * 0.1% = $0.50 < $1 minimum
        Money smallTrade = new Money(500m, "USD");
        Money smallCommission = commission.CalculateFor(smallTrade);
        smallCommission.Amount.ShouldBe(1.0m); // Minimum applies

        // Large trade: $100,000 * 0.1% = $100 > $1 minimum
        Money largeTrade = new Money(100000m, "USD");
        Money largeCommission = commission.CalculateFor(largeTrade);
        largeCommission.Amount.ShouldBe(100m); // Percentage applies
    }
}
