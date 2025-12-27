namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Represents a price per unit (e.g., price per share).
/// Distinct from Money to prevent mixing price-per-unit with total monetary amounts.
/// </summary>
public readonly record struct Price
{
    public decimal Value { get; init; }
    public string Currency { get; init; }

    public Price(decimal value, string currency = "USD")
    {
        if (value < 0)
        {
            throw new ArgumentException("Price cannot be negative.", nameof(value));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency cannot be null or whitespace.", nameof(currency));
        }

        Value = value;
        Currency = currency.ToUpperInvariant().Trim();
    }

    public static Price Zero => new(0m);
    public static Price FromDollars(decimal value) => new(value, "USD");

    // Convert price to money by multiplying by quantity
    public Money MultiplyBy(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
        }

        return new Money(Value * quantity, Currency);
    }

    public Money MultiplyBy(decimal quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
        }

        return new Money(Value * quantity, Currency);
    }

    // Comparison operators
    public static bool operator >(Price a, Price b)
    {
        EnsureSameCurrency(a, b);
        return a.Value > b.Value;
    }

    public static bool operator <(Price a, Price b)
    {
        EnsureSameCurrency(a, b);
        return a.Value < b.Value;
    }

    public static bool operator >=(Price a, Price b)
    {
        EnsureSameCurrency(a, b);
        return a.Value >= b.Value;
    }

    public static bool operator <=(Price a, Price b)
    {
        EnsureSameCurrency(a, b);
        return a.Value <= b.Value;
    }

    // Price difference
    public static Price operator -(Price a, Price b)
    {
        EnsureSameCurrency(a, b);
        decimal difference = a.Value - b.Value;

        if (difference < 0)
        {
            throw new InvalidOperationException("Price subtraction resulted in negative value.");
        }

        return new Price(difference, a.Currency);
    }

    // Percentage change
    public Percentage PercentageChangeTo(Price newPrice)
    {
        EnsureSameCurrency(this, newPrice);

        if (Value == 0)
        {
            return Percentage.Zero;
        }

        decimal change = ((newPrice.Value - Value) / Value) * 100m;
        return new Percentage(change);
    }

    // Implicit conversion to decimal for calculations
    public static implicit operator decimal(Price price) => price.Value;

    private static void EnsureSameCurrency(Price a, Price b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot compare prices in different currencies: {a.Currency} and {b.Currency}");
        }
    }

    public override string ToString() => $"{Value.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)} {Currency}";
    public string ToString(string format) => $"{Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture)} {Currency}";
}
