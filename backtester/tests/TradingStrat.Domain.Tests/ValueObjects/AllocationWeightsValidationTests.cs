using Shouldly;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.ValueObjects;

public class AllocationWeightsValidationTests
{
    #region IsValid Tests

    [Fact]
    public void AllocationWeights_WhenSumsTo100_IsValid()
    {
        // Arrange
        AllocationWeights weights = new(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 60m,
                ["MSFT"] = 30m
            },
            cashPercentage: 10m
        );

        // Act & Assert
        weights.IsValid().ShouldBeTrue();
    }

    [Theory]
    [InlineData(50, 40, 0)]  // 90%
    [InlineData(60, 50, 0)]  // 110%
    [InlineData(0, 0, 0)]    // 0%
    public void AllocationWeights_WhenDoesNotSumTo100_IsInvalid(
        decimal applePercent,
        decimal microsoftPercent,
        decimal cashPercent)
    {
        // Arrange
        AllocationWeights weights = new(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = applePercent,
                ["MSFT"] = microsoftPercent
            },
            cashPercentage: cashPercent
        );

        // Act & Assert
        weights.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void AllocationWeights_WithEmptyAllocations_IsInvalid()
    {
        // Arrange
        AllocationWeights weights = new(
            new Dictionary<string, decimal>(),
            cashPercentage: 0m
        );

        // Act & Assert
        weights.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void AllocationWeights_WithOnlyCash_IsValid()
    {
        // Arrange
        AllocationWeights weights = new(
            new Dictionary<string, decimal>(),
            cashPercentage: 100m
        );

        // Act & Assert
        weights.IsValid().ShouldBeTrue();
    }

    #endregion

    #region Negative Percentage Validation

    [Fact]
    public void AllocationWeights_WithNegativeTargetPercentage_ShouldThrow()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new AllocationWeights(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = -10m,
                ["MSFT"] = 110m
            },
            cashPercentage: 0m
        ));
    }

    [Fact]
    public void AllocationWeights_WithNegativeCashPercentage_ShouldThrow()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new AllocationWeights(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 50m,
                ["MSFT"] = 60m
            },
            cashPercentage: -10m
        ));
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void AllocationWeights_IsImmutable()
    {
        // Arrange
        AllocationWeights weights1 = new(
            new Dictionary<string, decimal> { ["AAPL"] = 60m },
            cashPercentage: 40m
        );

        AllocationWeights weights2 = new(
            new Dictionary<string, decimal> { ["AAPL"] = 60m },
            cashPercentage: 40m
        );

        // Act & Assert - Records support value equality
        weights1.ShouldBe(weights2);
    }

    #endregion

    #region Null Dictionary Validation

    [Fact]
    public void AllocationWeights_WithNullDictionary_ShouldThrow()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() => new AllocationWeights(
            null!,
            cashPercentage: 100m
        ));
    }

    #endregion
}
