namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Represents a monetary amount with currency.
/// Eliminates primitive obsession by wrapping decimal values that represent money.
/// </summary>
public readonly record struct Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency cannot be null or whitespace.", nameof(currency));
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant().Trim();
    }

    public static Money Zero => new(0m);
    public static Money FromDollars(decimal amount) => new(amount, "USD");

    // Arithmetic operators
    public static Money operator +(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator -(Money a)
    {
        return new Money(-a.Amount, a.Currency);
    }

    public static Money operator *(Money money, decimal multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }

    public static Money operator *(decimal multiplier, Money money)
    {
        return money * multiplier;
    }

    public static Money operator /(Money money, decimal divisor)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException("Cannot divide money by zero.");
        }

        return new Money(money.Amount / divisor, money.Currency);
    }

    // Comparison operators
    public static bool operator >(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Amount > b.Amount;
    }

    public static bool operator <(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Amount < b.Amount;
    }

    public static bool operator >=(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Amount >= b.Amount;
    }

    public static bool operator <=(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Amount <= b.Amount;
    }

    // Utility methods
    public Money MultiplyBy(decimal factor) => new(Amount * factor, Currency);
    public Money DivideBy(decimal divisor) => new(Amount / divisor, Currency);
    public Money Abs() => new(Math.Abs(Amount), Currency);

    public Percentage AsPercentageOf(Money total)
    {
        EnsureSameCurrency(this, total);

        if (total.Amount == 0)
        {
            return Percentage.Zero;
        }

        return Percentage.FromDecimal(Amount / total.Amount);
    }

    public static Money Max(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Amount >= b.Amount ? a : b;
    }

    public static Money Min(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Amount <= b.Amount ? a : b;
    }

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot perform operation on different currencies: {a.Currency} and {b.Currency}");
        }
    }

    public override string ToString() => $"{Amount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)} {Currency}";
    public string ToString(string format) => $"{Amount.ToString(format, System.Globalization.CultureInfo.InvariantCulture)} {Currency}";
}
