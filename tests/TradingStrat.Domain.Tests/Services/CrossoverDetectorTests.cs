using Shouldly;
using TradingStrat.Domain.Services;

namespace TradingStrat.Domain.Tests.Services;

public class CrossoverDetectorTests
{
    #region DetectCrossAbove - Single Series vs Threshold

    [Fact]
    public void DetectCrossAbove_WhenCrosses_ReturnsTrue()
    {
        // Arrange
        decimal[] values = new[] { 25m, 35m }; // crosses 30
        decimal threshold = 30m;

        // Act
        bool result = CrossoverDetector.DetectCrossAbove(values, 1, threshold);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void DetectCrossAbove_WhenAlreadyAbove_ReturnsFalse()
    {
        // Arrange
        decimal[] values = new[] { 35m, 40m }; // already above 30
        decimal threshold = 30m;

        // Act
        bool result = CrossoverDetector.DetectCrossAbove(values, 1, threshold);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void DetectCrossAbove_WhenStaysBelow_ReturnsFalse()
    {
        // Arrange
        decimal[] values = new[] { 20m, 25m }; // stays below 30
        decimal threshold = 30m;

        // Act
        bool result = CrossoverDetector.DetectCrossAbove(values, 1, threshold);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void DetectCrossAbove_WithInsufficientData_ReturnsFalse()
    {
        // Arrange
        decimal[] values = new[] { 35m };
        decimal threshold = 30m;

        // Act
        bool result = CrossoverDetector.DetectCrossAbove(values, 0, threshold);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region DetectCrossBelow - Single Series vs Threshold

    [Fact]
    public void DetectCrossBelow_WhenCrosses_ReturnsTrue()
    {
        // Arrange
        decimal[] values = new[] { 35m, 25m }; // crosses below 30
        decimal threshold = 30m;

        // Act
        bool result = CrossoverDetector.DetectCrossBelow(values, 1, threshold);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void DetectCrossBelow_WhenAlreadyBelow_ReturnsFalse()
    {
        // Arrange
        decimal[] values = new[] { 25m, 20m }; // already below 30
        decimal threshold = 30m;

        // Act
        bool result = CrossoverDetector.DetectCrossBelow(values, 1, threshold);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void DetectCrossBelow_WhenStaysAbove_ReturnsFalse()
    {
        // Arrange
        decimal[] values = new[] { 40m, 35m }; // stays above 30
        decimal threshold = 30m;

        // Act
        bool result = CrossoverDetector.DetectCrossBelow(values, 1, threshold);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void DetectCrossBelow_WithInsufficientData_ReturnsFalse()
    {
        // Arrange
        decimal[] values = new[] { 25m };
        decimal threshold = 30m;

        // Act
        bool result = CrossoverDetector.DetectCrossBelow(values, 0, threshold);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region DetectCrossBetween - Two Series

    [Fact]
    public void DetectCrossBetween_FastCrossesAboveSlow_ReturnsTrue()
    {
        // Arrange
        decimal[] fast = new[] { 95m, 105m };  // crosses above
        decimal[] slow = new[] { 100m, 100m };
        CrossDirection direction = CrossDirection.Above;

        // Act
        bool result = CrossoverDetector.DetectCrossBetween(fast, slow, 1, direction);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void DetectCrossBetween_FastCrossesBelowSlow_ReturnsTrue()
    {
        // Arrange
        decimal[] fast = new[] { 105m, 95m };  // crosses below
        decimal[] slow = new[] { 100m, 100m };
        CrossDirection direction = CrossDirection.Below;

        // Act
        bool result = CrossoverDetector.DetectCrossBetween(fast, slow, 1, direction);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void DetectCrossBetween_NoCrossover_ReturnsFalse()
    {
        // Arrange
        decimal[] fast = new[] { 105m, 110m };  // stays above
        decimal[] slow = new[] { 100m, 100m };
        CrossDirection direction = CrossDirection.Above;

        // Act
        bool result = CrossoverDetector.DetectCrossBetween(fast, slow, 1, direction);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void DetectCrossBetween_WithInsufficientData_ReturnsFalse()
    {
        // Arrange
        decimal[] fast = new[] { 105m };
        decimal[] slow = new[] { 100m };
        CrossDirection direction = CrossDirection.Above;

        // Act
        bool result = CrossoverDetector.DetectCrossBetween(fast, slow, 0, direction);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion
}
