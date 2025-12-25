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
            Ticker: "AAPL",
            StartDate: start,
            EndDate: end,
            InitialCapital: 10000m,
            CommissionPercentage: 0.1m,
            MinimumCommission: 1.0m
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
            Ticker: "AAPL",
            StartDate: start,
            EndDate: end,
            InitialCapital: 10000m,
            CommissionPercentage: 0.1m,
            MinimumCommission: 1.0m
        ));
    }

    [Fact]
    public void BacktestConfig_WithStartDateEqualToEndDate_ShouldThrow()
    {
        // Arrange
        DateTime date = DateTime.Today;

        // Act & Assert
        Should.Throw<ArgumentException>(() => new BacktestConfig(
            Ticker: "AAPL",
            StartDate: date,
            EndDate: date,
            InitialCapital: 10000m,
            CommissionPercentage: 0.1m,
            MinimumCommission: 1.0m
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
            Ticker: "AAPL",
            StartDate: start,
            EndDate: end,
            InitialCapital: 10000m,
            CommissionPercentage: 0.1m,
            MinimumCommission: 1.0m
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
            Ticker: "AAPL",
            StartDate: DateTime.Today.AddYears(-1),
            EndDate: DateTime.Today,
            InitialCapital: capital,
            CommissionPercentage: 0.1m,
            MinimumCommission: 1.0m
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
            Ticker: "AAPL",
            StartDate: DateTime.Today.AddYears(-1),
            EndDate: DateTime.Today,
            InitialCapital: capital,
            CommissionPercentage: 0.1m,
            MinimumCommission: 1.0m
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
            Ticker: "AAPL",
            StartDate: DateTime.Today.AddYears(-1),
            EndDate: DateTime.Today,
            InitialCapital: 10000m,
            CommissionPercentage: commissionPercent,
            MinimumCommission: 0m
        );

        // Assert
        config.CommissionPercentage.ShouldBe(commissionPercent);
    }

    [Fact]
    public void BacktestConfig_WithNegativeCommissionPercentage_ShouldThrow()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new BacktestConfig(
            Ticker: "AAPL",
            StartDate: DateTime.Today.AddYears(-1),
            EndDate: DateTime.Today,
            InitialCapital: 10000m,
            CommissionPercentage: -0.1m,
            MinimumCommission: 1.0m
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
            Ticker: "AAPL",
            StartDate: DateTime.Today.AddYears(-1),
            EndDate: DateTime.Today,
            InitialCapital: 10000m,
            CommissionPercentage: 0.1m,
            MinimumCommission: minCommission
        );

        // Assert
        config.MinimumCommission.ShouldBe(minCommission);
    }

    [Fact]
    public void BacktestConfig_WithNegativeMinimumCommission_ShouldThrow()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new BacktestConfig(
            Ticker: "AAPL",
            StartDate: DateTime.Today.AddYears(-1),
            EndDate: DateTime.Today,
            InitialCapital: 10000m,
            CommissionPercentage: 0.1m,
            MinimumCommission: -1.0m
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
            Ticker: ticker!,
            StartDate: DateTime.Today.AddYears(-1),
            EndDate: DateTime.Today,
            InitialCapital: 10000m,
            CommissionPercentage: 0.1m,
            MinimumCommission: 1.0m
        ));
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void BacktestConfig_IsImmutable()
    {
        // Arrange
        BacktestConfig config1 = new(
            Ticker: "AAPL",
            StartDate: new DateTime(2024, 1, 1),
            EndDate: new DateTime(2024, 12, 31),
            InitialCapital: 10000m,
            CommissionPercentage: 0.1m,
            MinimumCommission: 1.0m
        );

        BacktestConfig config2 = new(
            Ticker: "AAPL",
            StartDate: new DateTime(2024, 1, 1),
            EndDate: new DateTime(2024, 12, 31),
            InitialCapital: 10000m,
            CommissionPercentage: 0.1m,
            MinimumCommission: 1.0m
        );

        // Act & Assert - Records support value equality
        config1.ShouldBe(config2);
        (config1 == config2).ShouldBeTrue();
    }

    #endregion
}
