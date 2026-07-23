using Shouldly;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.ValueObjects;

public class PriceTests
{
    [Fact]
    public void Constructor_WithValidValue_CreatesInstance()
    {
        // Act
        Price price = new Price(150.50m);

        // Assert
        price.Value.ShouldBe(150.50m);
        price.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Constructor_WithNegativeValue_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new Price(-10m));
    }

    [Fact]
    public void Constructor_WithNullCurrency_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new Price(100m, null!));
    }

    [Fact]
    public void Constructor_WithLowercaseCurrency_NormalizesToUppercase()
    {
        // Act
        Price price = new Price(100m, "usd");

        // Assert
        price.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Zero_ReturnsZeroPrice()
    {
        // Act
        Price zero = Price.Zero;

        // Assert
        zero.Value.ShouldBe(0m);
    }

    [Fact]
    public void FromDollars_CreatesUSDPrice()
    {
        // Act
        Price price = Price.FromDollars(150m);

        // Assert
        price.Value.ShouldBe(150m);
        price.Currency.ShouldBe("USD");
    }

    [Fact]
    public void MultiplyBy_IntQuantity_ReturnsMoney()
    {
        // Arrange
        Price price = new Price(50m);

        // Act
        Money result = price.MultiplyBy(10);

        // Assert
        result.Amount.ShouldBe(500m);
        result.Currency.ShouldBe("USD");
    }

    [Fact]
    public void MultiplyBy_DecimalQuantity_ReturnsMoney()
    {
        // Arrange
        Price price = new Price(50m);

        // Act
        Money result = price.MultiplyBy(2.5m);

        // Assert
        result.Amount.ShouldBe(125m);
        result.Currency.ShouldBe("USD");
    }

    [Fact]
    public void MultiplyBy_NegativeQuantity_ThrowsArgumentException()
    {
        // Arrange
        Price price = new Price(50m);

        // Act & Assert
        Should.Throw<ArgumentException>(() => price.MultiplyBy(-10));
    }

    [Fact]
    public void MultiplyBy_NegativeDecimalQuantity_ThrowsArgumentException()
    {
        // Arrange
        Price price = new Price(50m);

        // Act & Assert
        Should.Throw<ArgumentException>(() => price.MultiplyBy(-2.5m));
    }

    [Fact]
    public void GreaterThan_WithSameCurrency_ComparesValues()
    {
        // Arrange
        Price a = new Price(150m);
        Price b = new Price(100m);

        // Act & Assert
        (a > b).ShouldBeTrue();
        (b > a).ShouldBeFalse();
    }

    [Fact]
    public void GreaterThan_WithDifferentCurrency_ThrowsInvalidOperationException()
    {
        // Arrange
        Price usd = new Price(150m);
        Price eur = new Price(100m, "EUR");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => usd > eur);
    }

    [Fact]
    public void LessThan_WithSameCurrency_ComparesValues()
    {
        // Arrange
        Price a = new Price(100m);
        Price b = new Price(150m);

        // Act & Assert
        (a < b).ShouldBeTrue();
        (b < a).ShouldBeFalse();
    }

    [Fact]
    public void GreaterThanOrEqual_WithSameCurrency_ComparesValues()
    {
        // Arrange
        Price a = new Price(150m);
        Price b = new Price(150m);
        Price c = new Price(100m);

        // Act & Assert
        (a >= b).ShouldBeTrue();
        (a >= c).ShouldBeTrue();
        (c >= a).ShouldBeFalse();
    }

    [Fact]
    public void LessThanOrEqual_WithSameCurrency_ComparesValues()
    {
        // Arrange
        Price a = new Price(100m);
        Price b = new Price(100m);
        Price c = new Price(150m);

        // Act & Assert
        (a <= b).ShouldBeTrue();
        (a <= c).ShouldBeTrue();
        (c <= a).ShouldBeFalse();
    }

    [Fact]
    public void Subtraction_WithSameCurrency_ReturnsDifference()
    {
        // Arrange
        Price a = new Price(150m);
        Price b = new Price(100m);

        // Act
        Price result = a - b;

        // Assert
        result.Value.ShouldBe(50m);
        result.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Subtraction_ResultingInNegative_ThrowsInvalidOperationException()
    {
        // Arrange
        Price a = new Price(100m);
        Price b = new Price(150m);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => a - b);
    }

    [Fact]
    public void PercentageChangeTo_CalculatesCorrectChange()
    {
        // Arrange
        Price oldPrice = new Price(100m);
        Price newPrice = new Price(120m);

        // Act
        Percentage change = oldPrice.PercentageChangeTo(newPrice);

        // Assert
        change.Value.ShouldBe(20m); // 20% increase
    }

    [Fact]
    public void PercentageChangeTo_WithDecrease_ReturnsNegativePercentage()
    {
        // Arrange
        Price oldPrice = new Price(100m);
        Price newPrice = new Price(80m);

        // Act
        Percentage change = oldPrice.PercentageChangeTo(newPrice);

        // Assert
        change.Value.ShouldBe(-20m); // 20% decrease
    }

    [Fact]
    public void PercentageChangeTo_WithZeroOldPrice_ReturnsZero()
    {
        // Arrange
        Price oldPrice = Price.Zero;
        Price newPrice = new Price(100m);

        // Act
        Percentage change = oldPrice.PercentageChangeTo(newPrice);

        // Assert
        change.Value.ShouldBe(0m);
    }

    [Fact]
    public void ImplicitConversion_ToDecimal_ReturnsValue()
    {
        // Arrange
        Price price = new Price(150.50m);

        // Act
        decimal value = price;

        // Assert
        value.ShouldBe(150.50m);
    }

    [Fact]
    public void ToString_FormatsWithCurrency()
    {
        // Arrange
        Price price = new Price(1234.56m);

        // Act
        string result = price.ToString();

        // Assert
        result.ShouldBe("1,234.56 USD");
    }

    [Fact]
    public void ToString_WithFormat_UsesCustomFormat()
    {
        // Arrange
        Price price = new Price(1234.567m);

        // Act
        string result = price.ToString("F3");

        // Assert
        result.ShouldBe("1234.567 USD");
    }
}
