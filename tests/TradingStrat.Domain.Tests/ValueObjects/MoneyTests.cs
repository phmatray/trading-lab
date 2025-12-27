using Shouldly;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithValidAmount_CreatesInstance()
    {
        // Act
        Money money = new Money(100.50m, "USD");

        // Assert
        money.Amount.ShouldBe(100.50m);
        money.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Constructor_WithNullCurrency_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new Money(100m, null!));
    }

    [Fact]
    public void Constructor_WithEmptyCurrency_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new Money(100m, ""));
    }

    [Fact]
    public void Constructor_WithLowercaseCurrency_NormalizesToUppercase()
    {
        // Act
        Money money = new Money(100m, "usd");

        // Assert
        money.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Constructor_WithWhitespaceCurrency_TrimsCurrency()
    {
        // Act
        Money money = new Money(100m, " USD ");

        // Assert
        money.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Zero_ReturnsZeroAmount()
    {
        // Act
        Money zero = Money.Zero;

        // Assert
        zero.Amount.ShouldBe(0m);
    }

    [Fact]
    public void FromDollars_CreatesUSDMoney()
    {
        // Act
        Money money = Money.FromDollars(100m);

        // Assert
        money.Amount.ShouldBe(100m);
        money.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Addition_WithSameCurrency_ReturnsSum()
    {
        // Arrange
        Money a = new Money(100m, "USD");
        Money b = new Money(50m, "USD");

        // Act
        Money result = a + b;

        // Assert
        result.Amount.ShouldBe(150m);
        result.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Addition_WithDifferentCurrency_ThrowsInvalidOperationException()
    {
        // Arrange
        Money usd = new Money(100m, "USD");
        Money eur = new Money(50m, "EUR");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => usd + eur);
    }

    [Fact]
    public void Subtraction_WithSameCurrency_ReturnsDifference()
    {
        // Arrange
        Money a = new Money(100m, "USD");
        Money b = new Money(30m, "USD");

        // Act
        Money result = a - b;

        // Assert
        result.Amount.ShouldBe(70m);
        result.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Subtraction_WithDifferentCurrency_ThrowsInvalidOperationException()
    {
        // Arrange
        Money usd = new Money(100m, "USD");
        Money eur = new Money(50m, "EUR");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => usd - eur);
    }

    [Fact]
    public void UnaryMinus_ReturnsNegatedAmount()
    {
        // Arrange
        Money money = new Money(100m, "USD");

        // Act
        Money result = -money;

        // Assert
        result.Amount.ShouldBe(-100m);
        result.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Multiplication_ByDecimal_ReturnsScaledAmount()
    {
        // Arrange
        Money money = new Money(100m, "USD");

        // Act
        Money result = money * 2.5m;

        // Assert
        result.Amount.ShouldBe(250m);
        result.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Multiplication_DecimalByMoney_ReturnsScaledAmount()
    {
        // Arrange
        Money money = new Money(100m, "USD");

        // Act
        Money result = 2.5m * money;

        // Assert
        result.Amount.ShouldBe(250m);
        result.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Division_ByDecimal_ReturnsScaledAmount()
    {
        // Arrange
        Money money = new Money(100m, "USD");

        // Act
        Money result = money / 4m;

        // Assert
        result.Amount.ShouldBe(25m);
        result.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Division_ByZero_ThrowsDivideByZeroException()
    {
        // Arrange
        Money money = new Money(100m, "USD");

        // Act & Assert
        Should.Throw<DivideByZeroException>(() => money / 0m);
    }

    [Fact]
    public void GreaterThan_WithSameCurrency_ComparesAmounts()
    {
        // Arrange
        Money a = new Money(100m, "USD");
        Money b = new Money(50m, "USD");

        // Act & Assert
        (a > b).ShouldBeTrue();
        (b > a).ShouldBeFalse();
    }

    [Fact]
    public void GreaterThan_WithDifferentCurrency_ThrowsInvalidOperationException()
    {
        // Arrange
        Money usd = new Money(100m, "USD");
        Money eur = new Money(50m, "EUR");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => usd > eur);
    }

    [Fact]
    public void LessThan_WithSameCurrency_ComparesAmounts()
    {
        // Arrange
        Money a = new Money(50m, "USD");
        Money b = new Money(100m, "USD");

        // Act & Assert
        (a < b).ShouldBeTrue();
        (b < a).ShouldBeFalse();
    }

    [Fact]
    public void MultiplyBy_ReturnsScaledAmount()
    {
        // Arrange
        Money money = new Money(100m, "USD");

        // Act
        Money result = money.MultiplyBy(3m);

        // Assert
        result.Amount.ShouldBe(300m);
    }

    [Fact]
    public void DivideBy_ReturnsScaledAmount()
    {
        // Arrange
        Money money = new Money(100m, "USD");

        // Act
        Money result = money.DivideBy(2m);

        // Assert
        result.Amount.ShouldBe(50m);
    }

    [Fact]
    public void Abs_WithNegativeAmount_ReturnsAbsoluteValue()
    {
        // Arrange
        Money money = new Money(-100m, "USD");

        // Act
        Money result = money.Abs();

        // Assert
        result.Amount.ShouldBe(100m);
        result.Currency.ShouldBe("USD");
    }

    [Fact]
    public void AsPercentageOf_CalculatesCorrectPercentage()
    {
        // Arrange
        Money part = new Money(25m, "USD");
        Money total = new Money(100m, "USD");

        // Act
        Percentage result = part.AsPercentageOf(total);

        // Assert
        result.Value.ShouldBe(25m);
    }

    [Fact]
    public void AsPercentageOf_WithZeroTotal_ReturnsZero()
    {
        // Arrange
        Money part = new Money(25m, "USD");
        Money total = Money.Zero;

        // Act
        Percentage result = part.AsPercentageOf(total);

        // Assert
        result.Value.ShouldBe(0m);
    }

    [Fact]
    public void Max_ReturnsBiggerAmount()
    {
        // Arrange
        Money a = new Money(100m, "USD");
        Money b = new Money(150m, "USD");

        // Act
        Money result = Money.Max(a, b);

        // Assert
        result.Amount.ShouldBe(150m);
    }

    [Fact]
    public void Min_ReturnsSmallerAmount()
    {
        // Arrange
        Money a = new Money(100m, "USD");
        Money b = new Money(50m, "USD");

        // Act
        Money result = Money.Min(a, b);

        // Assert
        result.Amount.ShouldBe(50m);
    }

    [Fact]
    public void ToString_FormatsWithCurrency()
    {
        // Arrange
        Money money = new Money(1234.56m, "USD");

        // Act
        string result = money.ToString();

        // Assert
        result.ShouldBe("1,234.56 USD");
    }

    [Fact]
    public void ToString_WithFormat_UsesCustomFormat()
    {
        // Arrange
        Money money = new Money(1234.567m, "USD");

        // Act
        string result = money.ToString("F3");

        // Assert
        result.ShouldBe("1234.567 USD");
    }
}
