using Shouldly;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.ValueObjects;

public class PercentageTests
{
    [Fact]
    public void Constructor_WithValue_CreatesInstance()
    {
        // Act
        Percentage percentage = new Percentage(25m);

        // Assert
        percentage.Value.ShouldBe(25m);
    }

    [Fact]
    public void Zero_ReturnsZeroPercentage()
    {
        // Act
        Percentage zero = Percentage.Zero;

        // Assert
        zero.Value.ShouldBe(0m);
    }

    [Fact]
    public void FromPercentage_CreatesInstanceWithSameValue()
    {
        // Act
        Percentage percentage = Percentage.FromPercentage(50m);

        // Assert
        percentage.Value.ShouldBe(50m);
    }

    [Fact]
    public void FromDecimal_ConvertsDecimalToPercentage()
    {
        // Act
        Percentage percentage = Percentage.FromDecimal(0.25m);

        // Assert
        percentage.Value.ShouldBe(25m);
    }

    [Fact]
    public void AsDecimal_ConvertsPercentageToDecimal()
    {
        // Arrange
        Percentage percentage = new Percentage(25m);

        // Act
        decimal result = percentage.AsDecimal();

        // Assert
        result.ShouldBe(0.25m);
    }

    [Fact]
    public void AsPercentage_ReturnsValue()
    {
        // Arrange
        Percentage percentage = new Percentage(25m);

        // Act
        decimal result = percentage.AsPercentage();

        // Assert
        result.ShouldBe(25m);
    }

    [Fact]
    public void Addition_ReturnsSumOfPercentages()
    {
        // Arrange
        Percentage a = new Percentage(25m);
        Percentage b = new Percentage(15m);

        // Act
        Percentage result = a + b;

        // Assert
        result.Value.ShouldBe(40m);
    }

    [Fact]
    public void Subtraction_ReturnsDifferenceOfPercentages()
    {
        // Arrange
        Percentage a = new Percentage(50m);
        Percentage b = new Percentage(20m);

        // Act
        Percentage result = a - b;

        // Assert
        result.Value.ShouldBe(30m);
    }

    [Fact]
    public void Multiplication_ByDecimal_ReturnsScaledPercentage()
    {
        // Arrange
        Percentage percentage = new Percentage(20m);

        // Act
        Percentage result = percentage * 2.5m;

        // Assert
        result.Value.ShouldBe(50m);
    }

    [Fact]
    public void Division_ByDecimal_ReturnsScaledPercentage()
    {
        // Arrange
        Percentage percentage = new Percentage(50m);

        // Act
        Percentage result = percentage / 2m;

        // Assert
        result.Value.ShouldBe(25m);
    }

    [Fact]
    public void Division_ByZero_ThrowsDivideByZeroException()
    {
        // Arrange
        Percentage percentage = new Percentage(50m);

        // Act & Assert
        Should.Throw<DivideByZeroException>(() => percentage / 0m);
    }

    [Fact]
    public void GreaterThan_ComparesValues()
    {
        // Arrange
        Percentage a = new Percentage(50m);
        Percentage b = new Percentage(30m);

        // Act & Assert
        (a > b).ShouldBeTrue();
        (b > a).ShouldBeFalse();
    }

    [Fact]
    public void LessThan_ComparesValues()
    {
        // Arrange
        Percentage a = new Percentage(30m);
        Percentage b = new Percentage(50m);

        // Act & Assert
        (a < b).ShouldBeTrue();
        (b < a).ShouldBeFalse();
    }

    [Fact]
    public void GreaterThanOrEqual_ComparesValues()
    {
        // Arrange
        Percentage a = new Percentage(50m);
        Percentage b = new Percentage(50m);
        Percentage c = new Percentage(30m);

        // Act & Assert
        (a >= b).ShouldBeTrue();
        (a >= c).ShouldBeTrue();
        (c >= a).ShouldBeFalse();
    }

    [Fact]
    public void LessThanOrEqual_ComparesValues()
    {
        // Arrange
        Percentage a = new Percentage(30m);
        Percentage b = new Percentage(30m);
        Percentage c = new Percentage(50m);

        // Act & Assert
        (a <= b).ShouldBeTrue();
        (a <= c).ShouldBeTrue();
        (c <= a).ShouldBeFalse();
    }

    [Fact]
    public void Of_AppliesPercentageToMoney()
    {
        // Arrange
        Percentage percentage = new Percentage(10m); // 10%
        Money amount = new Money(1000m);

        // Act
        Money result = percentage.Of(amount);

        // Assert
        result.Amount.ShouldBe(100m); // 10% of 1000
        result.Currency.ShouldBe("USD");
    }

    [Fact]
    public void UnaryMinus_ReturnsNegatedPercentage()
    {
        // Arrange
        Percentage percentage = new Percentage(25m);

        // Act
        Percentage result = -percentage;

        // Assert
        result.Value.ShouldBe(-25m);
    }

    [Fact]
    public void Abs_WithNegativeValue_ReturnsAbsoluteValue()
    {
        // Arrange
        Percentage percentage = new Percentage(-25m);

        // Act
        Percentage result = percentage.Abs();

        // Assert
        result.Value.ShouldBe(25m);
    }

    [Fact]
    public void Abs_WithPositiveValue_ReturnsSameValue()
    {
        // Arrange
        Percentage percentage = new Percentage(25m);

        // Act
        Percentage result = percentage.Abs();

        // Assert
        result.Value.ShouldBe(25m);
    }

    [Fact]
    public void ToString_FormatsWithPercentSign()
    {
        // Arrange
        Percentage percentage = new Percentage(25.50m);

        // Act
        string result = percentage.ToString();

        // Assert
        result.ShouldBe("25.50%");
    }

    [Fact]
    public void ToString_WithFormat_UsesCustomFormat()
    {
        // Arrange
        Percentage percentage = new Percentage(25.567m);

        // Act
        string result = percentage.ToString("F3");

        // Assert
        result.ShouldBe("25.567%");
    }
}
