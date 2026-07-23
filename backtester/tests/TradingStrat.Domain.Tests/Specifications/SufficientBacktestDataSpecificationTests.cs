using Shouldly;
using TradingStrat.Domain.Specifications;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Specifications;

public class SufficientBacktestDataSpecificationTests
{
    #region Sufficient Data

    [Fact]
    public void IsSatisfiedBy_WithSufficientData_ReturnsTrue()
    {
        // Arrange
        var specification = new SufficientBacktestDataSpecification(minimumBars: 100);
        BacktestConfig config = new(
            ticker: "AAPL",
            startDate: DateTime.Today.AddDays(-150),
            endDate: DateTime.Today,
            initialCapital: 10000m,
            commissionPercentage: 0.1m,
            minimumCommission: 1.0m
        );

        // Act
        bool result = specification.IsSatisfiedBy(config);

        // Assert
        result.ShouldBeTrue();
        specification.Reason.ShouldBeEmpty();
    }

    [Fact]
    public void IsSatisfiedBy_WithExactlyMinimumBars_ReturnsTrue()
    {
        // Arrange
        var specification = new SufficientBacktestDataSpecification(minimumBars: 100);
        BacktestConfig config = new(
            ticker: "AAPL",
            startDate: DateTime.Today.AddDays(-100),
            endDate: DateTime.Today,
            initialCapital: 10000m,
            commissionPercentage: 0.1m,
            minimumCommission: 1.0m
        );

        // Act
        bool result = specification.IsSatisfiedBy(config);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region Insufficient Data

    [Fact]
    public void IsSatisfiedBy_WithInsufficientData_ReturnsFalse()
    {
        // Arrange
        var specification = new SufficientBacktestDataSpecification(minimumBars: 100);
        BacktestConfig config = new(
            ticker: "AAPL",
            startDate: DateTime.Today.AddDays(-50),
            endDate: DateTime.Today,
            initialCapital: 10000m,
            commissionPercentage: 0.1m,
            minimumCommission: 1.0m
        );

        // Act
        bool result = specification.IsSatisfiedBy(config);

        // Assert
        result.ShouldBeFalse();
        specification.Reason.ShouldContain("Insufficient data");
        specification.Reason.ShouldContain("50 days");
        specification.Reason.ShouldContain("need 100");
    }

    #endregion

    #region Different Minimum Requirements

    [Theory]
    [InlineData(50, 60, true)]
    [InlineData(50, 49, false)]
    [InlineData(200, 250, true)]
    [InlineData(200, 150, false)]
    public void IsSatisfiedBy_WithVariousMinimums_ReturnsCorrectResult(int minimumBars, int actualDays, bool expected)
    {
        // Arrange
        var specification = new SufficientBacktestDataSpecification(minimumBars);
        BacktestConfig config = new(
            ticker: "AAPL",
            startDate: DateTime.Today.AddDays(-actualDays),
            endDate: DateTime.Today,
            initialCapital: 10000m,
            commissionPercentage: 0.1m,
            minimumCommission: 1.0m
        );

        // Act
        bool result = specification.IsSatisfiedBy(config);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion
}
