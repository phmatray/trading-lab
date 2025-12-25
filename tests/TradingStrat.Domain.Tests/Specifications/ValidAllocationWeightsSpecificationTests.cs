using Shouldly;
using TradingStrat.Domain.Specifications;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Specifications;

public class ValidAllocationWeightsSpecificationTests
{
    private readonly ValidAllocationWeightsSpecification _specification;

    public ValidAllocationWeightsSpecificationTests()
    {
        _specification = new ValidAllocationWeightsSpecification();
    }

    #region Valid Allocations

    [Fact]
    public void IsSatisfiedBy_WithValidAllocations100Percent_ReturnsTrue()
    {
        // Arrange
        AllocationWeights weights = new(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 60m,
                ["MSFT"] = 40m
            },
            CashPercentage: 0m
        );

        // Act
        bool result = _specification.IsSatisfiedBy(weights);

        // Assert
        result.ShouldBeTrue();
        _specification.Reason.ShouldBeEmpty();
    }

    [Fact]
    public void IsSatisfiedBy_WithValidAllocationsIncludingCash_ReturnsTrue()
    {
        // Arrange
        AllocationWeights weights = new(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 50m,
                ["MSFT"] = 30m
            },
            CashPercentage: 20m
        );

        // Act
        bool result = _specification.IsSatisfiedBy(weights);

        // Assert
        result.ShouldBeTrue();
        _specification.Reason.ShouldBeEmpty();
    }

    [Fact]
    public void IsSatisfiedBy_With100PercentCash_ReturnsTrue()
    {
        // Arrange
        AllocationWeights weights = new(
            new Dictionary<string, decimal>(),
            CashPercentage: 100m
        );

        // Act
        bool result = _specification.IsSatisfiedBy(weights);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region Invalid Allocations

    [Fact]
    public void IsSatisfiedBy_WithTotalLessThan100_ReturnsFalse()
    {
        // Arrange
        AllocationWeights weights = new(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 50m,
                ["MSFT"] = 40m
            },
            CashPercentage: 0m
        );

        // Act
        bool result = _specification.IsSatisfiedBy(weights);

        // Assert
        result.ShouldBeFalse();
        _specification.Reason.ShouldContain("must sum to 100%");
        _specification.Reason.ShouldContain("got 90");
    }

    [Fact]
    public void IsSatisfiedBy_WithTotalGreaterThan100_ReturnsFalse()
    {
        // Arrange
        AllocationWeights weights = new(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 60m,
                ["MSFT"] = 50m
            },
            CashPercentage: 0m
        );

        // Act
        bool result = _specification.IsSatisfiedBy(weights);

        // Assert
        result.ShouldBeFalse();
        _specification.Reason.ShouldContain("must sum to 100%");
        _specification.Reason.ShouldContain("got 110");
    }

    [Fact]
    public void IsSatisfiedBy_WithNegativePercentage_ThrowsDuringConstruction()
    {
        // Arrange & Act & Assert - Constructor validation prevents negative percentages
        Should.Throw<ArgumentException>(() => new AllocationWeights(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = -10m,
                ["MSFT"] = 110m
            },
            CashPercentage: 0m
        ));
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData(99.99)]
    [InlineData(100.01)]
    public void IsSatisfiedBy_WithinToleranceOf100_ReturnsTrue(decimal total)
    {
        // Arrange - Test floating point tolerance (0.01%)
        AllocationWeights weights = new(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = total
            },
            CashPercentage: 0m
        );

        // Act
        bool result = _specification.IsSatisfiedBy(weights);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion
}
