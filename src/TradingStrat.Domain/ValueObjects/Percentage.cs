using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Represents a percentage value (stored as 0-100).
/// Eliminates confusion between decimal representation (0.01) and percentage representation (1%).
/// </summary>
public sealed class Percentage : ValueObject
{
    public decimal Value { get; init; }

    public Percentage(decimal value)
    {
        Value = value;
    }

    public static Percentage Zero => new(0m);

    // Factory methods for clarity
    public static Percentage FromPercentage(decimal percentage) => new(percentage);
    public static Percentage FromDecimal(decimal decimalValue) => new(decimalValue * 100m);

    // Conversion methods
    public decimal AsDecimal() => Value / 100m;
    public decimal AsPercentage() => Value;

    // Arithmetic operators
    public static Percentage operator +(Percentage a, Percentage b)
    {
        return new Percentage(a.Value + b.Value);
    }

    public static Percentage operator -(Percentage a, Percentage b)
    {
        return new Percentage(a.Value - b.Value);
    }

    public static Percentage operator *(Percentage percentage, decimal multiplier)
    {
        return new Percentage(percentage.Value * multiplier);
    }

    public static Percentage operator /(Percentage percentage, decimal divisor)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException("Cannot divide percentage by zero.");
        }

        return new Percentage(percentage.Value / divisor);
    }

    // Comparison operators
    public static bool operator >(Percentage a, Percentage b) => a.Value > b.Value;
    public static bool operator <(Percentage a, Percentage b) => a.Value < b.Value;
    public static bool operator >=(Percentage a, Percentage b) => a.Value >= b.Value;
    public static bool operator <=(Percentage a, Percentage b) => a.Value <= b.Value;

    // Apply percentage to a money amount
    public Money Of(Money amount)
    {
        return amount.MultiplyBy(AsDecimal());
    }

    // Negate
    public static Percentage operator -(Percentage p)
    {
        return new Percentage(-p.Value);
    }

    // Absolute value
    public Percentage Abs() => new(Math.Abs(Value));

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)}%";
    public string ToString(string format) => $"{Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture)}%";
}
