using Shouldly;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.ValueObjects;

public class TimeFrameValidationTests
{
    #region Equality Tests

    [Fact]
    public void TimeFrame_WithSameUnit_AreEqual()
    {
        // Arrange
        TimeFrame timeFrame1 = new() { Unit = TimeFrameUnit.D1 };
        TimeFrame timeFrame2 = new() { Unit = TimeFrameUnit.D1 };

        // Act & Assert
        timeFrame1.ShouldBe(timeFrame2);
        (timeFrame1 == timeFrame2).ShouldBeTrue();
    }

    [Fact]
    public void TimeFrame_StaticInstances_AreEqual()
    {
        // Arrange & Act
        TimeFrame d1_1 = TimeFrame.D1;
        TimeFrame d1_2 = TimeFrame.D1;

        // Assert
        d1_1.ShouldBe(d1_2);
        ReferenceEquals(d1_1, d1_2).ShouldBeTrue(); // Flyweight pattern
    }

    [Fact]
    public void TimeFrame_WithDifferentUnits_AreNotEqual()
    {
        // Arrange
        TimeFrame d1 = TimeFrame.D1;
        TimeFrame h1 = TimeFrame.H1;

        // Act & Assert
        d1.ShouldNotBe(h1);
        (d1 != h1).ShouldBeTrue();
    }

    #endregion

    #region ToString Tests

    [Theory]
    [InlineData(TimeFrameUnit.M1, "M1")]
    [InlineData(TimeFrameUnit.M5, "M5")]
    [InlineData(TimeFrameUnit.H1, "H1")]
    [InlineData(TimeFrameUnit.D1, "D1")]
    [InlineData(TimeFrameUnit.W1, "W1")]
    [InlineData(TimeFrameUnit.MN1, "MN1")]
    public void TimeFrame_ToString_ReturnsCorrectFormat(TimeFrameUnit unit, string expected)
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = unit };

        // Act
        string result = timeFrame.ToString();

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region FromString Tests

    [Theory]
    [InlineData("M1", TimeFrameUnit.M1)]
    [InlineData("M5", TimeFrameUnit.M5)]
    [InlineData("H1", TimeFrameUnit.H1)]
    [InlineData("D1", TimeFrameUnit.D1)]
    [InlineData("W1", TimeFrameUnit.W1)]
    [InlineData("MN1", TimeFrameUnit.MN1)]
    [InlineData("m1", TimeFrameUnit.M1)]  // Case insensitive
    [InlineData("d1", TimeFrameUnit.D1)]  // Case insensitive
    public void TimeFrame_FromString_ParsesCorrectly(string input, TimeFrameUnit expected)
    {
        // Act
        TimeFrame result = TimeFrame.FromString(input);

        // Assert
        result.Unit.ShouldBe(expected);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("X1")]
    [InlineData("")]
    [InlineData("   ")]
    public void TimeFrame_FromString_WithInvalidValue_ThrowsArgumentException(string input)
    {
        // Arrange & Act & Assert
        ArgumentException ex = Should.Throw<ArgumentException>(() => TimeFrame.FromString(input));
        ex.Message.ShouldContain("Invalid timeframe");
    }

    [Fact]
    public void TimeFrame_FromString_WithNull_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() => TimeFrame.FromString(null!));
    }

    #endregion

    #region Conversion Methods

    [Theory]
    [InlineData(TimeFrameUnit.M1, 1)]
    [InlineData(TimeFrameUnit.M5, 5)]
    [InlineData(TimeFrameUnit.H1, 60)]
    [InlineData(TimeFrameUnit.D1, 1440)]
    [InlineData(TimeFrameUnit.W1, 10080)]
    public void TimeFrame_ToMinutes_ReturnsCorrectValue(TimeFrameUnit unit, int expectedMinutes)
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = unit };

        // Act
        int result = timeFrame.ToMinutes();

        // Assert
        result.ShouldBe(expectedMinutes);
    }

    [Fact]
    public void TimeFrame_ToTimeSpan_ReturnsCorrectValue()
    {
        // Arrange
        TimeFrame timeFrame = TimeFrame.H1;

        // Act
        TimeSpan result = timeFrame.ToTimeSpan();

        // Assert
        result.ShouldBe(TimeSpan.FromHours(1));
    }

    #endregion

    #region Classification Methods

    [Theory]
    [InlineData(TimeFrameUnit.M1, true)]
    [InlineData(TimeFrameUnit.M5, true)]
    [InlineData(TimeFrameUnit.H1, true)]
    [InlineData(TimeFrameUnit.D1, false)]
    [InlineData(TimeFrameUnit.W1, false)]
    public void TimeFrame_IsIntraday_ReturnsCorrectValue(TimeFrameUnit unit, bool expected)
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = unit };

        // Act
        bool result = timeFrame.IsIntraday();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(TimeFrameUnit.M1, false)]
    [InlineData(TimeFrameUnit.H1, false)]
    [InlineData(TimeFrameUnit.D1, true)]
    [InlineData(TimeFrameUnit.W1, false)]
    public void TimeFrame_IsDaily_ReturnsCorrectValue(TimeFrameUnit unit, bool expected)
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = unit };

        // Act
        bool result = timeFrame.IsDaily();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(TimeFrameUnit.D1, false)]
    [InlineData(TimeFrameUnit.W1, true)]
    [InlineData(TimeFrameUnit.MN1, true)]
    public void TimeFrame_IsHigherThanDaily_ReturnsCorrectValue(TimeFrameUnit unit, bool expected)
    {
        // Arrange
        TimeFrame timeFrame = new() { Unit = unit };

        // Act
        bool result = timeFrame.IsHigherThanDaily();

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region Period Multiplier Tests

    [Fact]
    public void TimeFrame_GetPeriodMultiplier_CalculatesCorrectly()
    {
        // Arrange
        TimeFrame d1 = TimeFrame.D1;
        TimeFrame h1 = TimeFrame.H1;

        // Act
        int multiplier = d1.GetPeriodMultiplier(h1); // D1 / H1 = 1440 / 60 = 24

        // Assert
        multiplier.ShouldBe(24);
    }

    [Fact]
    public void TimeFrame_GetPeriodMultiplier_WithZeroReference_Throws()
    {
        // Arrange
        TimeFrame timeFrame = TimeFrame.D1;
        TimeFrame zeroTimeFrame = new() { Unit = 0 };

        // Act & Assert
        Should.Throw<ArgumentException>(() => timeFrame.GetPeriodMultiplier(zeroTimeFrame));
    }

    #endregion
}
