using Shouldly;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.ValueObjects;

public class BacktestConfigValidationTests
{
    #region Date Range Validation

    [Fact]
    public void BacktestConfig_WithValidDateRange_ShouldBeCreated()
    {
        // Arrange
        DateTime start = DateTime.Today.AddYears(-1);
        DateTime end = DateTime.Today;

        // Act
        BacktestConfig config = new(
            ticker: "AAPL",
            startDate: start,
            endDate: end,
            initialCapital: 10000m,
            commissionPercentage: 0.1m,
            minimumCommission: 1.0m
        );

        // Assert
        config.StartDate.ShouldBe(start);
        config.EndDate.ShouldBe(end);
    }

    [Fact]
    public void BacktestConfig_WithStartDateAfterEndDate_ShouldThrow()
    {
        // Arrange
        DateTime start = DateTime.Today;
        DateTime end = DateTime.Today.AddDays(-10);

        // Act & Assert
        Should.Throw<ArgumentException>(() => new BacktestConfig(
            ticker: "AAPL",
            startDate: start,
            endDate: end,
            initialCapital: 10000m,
            commissionPercentage: 0.1m,
            minimumCommission: 1.0m
        ));
    }

    [Fact]
    public void BacktestConfig_WithStartDateEqualToEndDate_ShouldThrow()
    {
        // Arrange
        DateTime date = DateTime.Today;

        // Act & Assert
        Should.Throw<ArgumentException>(() => new BacktestConfig(
            ticker: "AAPL",
            startDate: date,
            endDate: date,
            initialCapital: 10000m,
            commissionPercentage: 0.1m,
            minimumCommission: 1.0m
        ));
    }

    [Fact]
    public void BacktestConfig_WithFutureStartDate_ShouldThrow()
    {
        // Arrange
        DateTime start = DateTime.Today.AddDays(1);
        DateTime end = DateTime.Today.AddDays(10);

        // Act & Assert
        Should.Throw<ArgumentException>(() => new BacktestConfig(
            ticker: "AAPL",
            startDate: start,
            endDate: end,
            initialCapital: 10000m,
            commissionPercentage: 0.1m,
            minimumCommission: 1.0m
        ));
    }

    #endregion

    #region InitialCapital Validation

    [Theory]
    [InlineData(0.01)]
    [InlineData(100)]
    [InlineData(1000000)]
    public void BacktestConfig_WithPositiveInitialCapital_ShouldBeCreated(decimal capital)
    {
        // Arrange & Act
        BacktestConfig config = new(
            ticker: "AAPL",
            startDate: DateTime.Today.AddYears(-1),
            endDate: DateTime.Today,
            initialCapital: capital,
            commissionPercentage: 0.1m,
            minimumCommission: 1.0m
        );

        // Assert
        config.InitialCapital.ShouldBe(capital);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10000)]
    public void BacktestConfig_WithInvalidInitialCapital_ShouldThrow(decimal capital)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new BacktestConfig(
            ticker: "AAPL",
            startDate: DateTime.Today.AddYears(-1),
            endDate: DateTime.Today,
            initialCapital: capital,
            commissionPercentage: 0.1m,
            minimumCommission: 1.0m
        ));
    }

    #endregion

    #region Commission Validation

    [Theory]
    [InlineData(0)]
    [InlineData(0.05)]
    [InlineData(1.0)]
    public void BacktestConfig_WithValidCommissionPercentage_ShouldBeCreated(decimal commissionPercent)
    {
        // Arrange & Act
        BacktestConfig config = new(
            ticker: "AAPL",
            startDate: DateTime.Today.AddYears(-1),
            endDate: DateTime.Today,
            initialCapital: 10000m,
            commissionPercentage: commissionPercent,
            minimumCommission: 0m
        );

        // Assert
        config.CommissionPercentage.ShouldBe(commissionPercent);
    }

    [Fact]
    public void BacktestConfig_WithNegativeCommissionPercentage_ShouldThrow()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new BacktestConfig(
            ticker: "AAPL",
            startDate: DateTime.Today.AddYears(-1),
            endDate: DateTime.Today,
            initialCapital: 10000m,
            commissionPercentage: -0.1m,
            minimumCommission: 1.0m
        ));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(10)]
    public void BacktestConfig_WithValidMinimumCommission_ShouldBeCreated(decimal minCommission)
    {
        // Arrange & Act
        BacktestConfig config = new(
            ticker: "AAPL",
            startDate: DateTime.Today.AddYears(-1),
            endDate: DateTime.Today,
            initialCapital: 10000m,
            commissionPercentage: 0.1m,
            minimumCommission: minCommission
        );

        // Assert
        config.MinimumCommission.ShouldBe(minCommission);
    }

    [Fact]
    public void BacktestConfig_WithNegativeMinimumCommission_ShouldThrow()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new BacktestConfig(
            ticker: "AAPL",
            startDate: DateTime.Today.AddYears(-1),
            endDate: DateTime.Today,
            initialCapital: 10000m,
            commissionPercentage: 0.1m,
            minimumCommission: -1.0m
        ));
    }

    #endregion

    #region Ticker Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BacktestConfig_WithInvalidTicker_ShouldThrow(string? ticker)
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new BacktestConfig(
            ticker: ticker!,
            startDate: DateTime.Today.AddYears(-1),
            endDate: DateTime.Today,
            initialCapital: 10000m,
            commissionPercentage: 0.1m,
            minimumCommission: 1.0m
        ));
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void BacktestConfig_IsImmutable()
    {
        // Arrange
        BacktestConfig config1 = new(
            ticker: "AAPL",
            startDate: new DateTime(2024, 1, 1),
            endDate: new DateTime(2024, 12, 31),
            initialCapital: 10000m,
            commissionPercentage: 0.1m,
            minimumCommission: 1.0m
        );

        BacktestConfig config2 = new(
            ticker: "AAPL",
            startDate: new DateTime(2024, 1, 1),
            endDate: new DateTime(2024, 12, 31),
            initialCapital: 10000m,
            commissionPercentage: 0.1m,
            minimumCommission: 1.0m
        );

        // Act & Assert - Records support value equality
        config1.ShouldBe(config2);
        (config1 == config2).ShouldBeTrue();
    }

    #endregion
}
