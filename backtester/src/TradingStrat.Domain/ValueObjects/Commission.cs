using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Represents commission structure with a percentage rate and minimum amount.
/// Encapsulates commission calculation logic.
/// </summary>
public sealed class Commission : ValueObject
{
    public Percentage Rate { get; init; }
    public Money Minimum { get; init; }

    public Commission(Percentage rate, Money minimum)
    {
        if (rate.Value < 0)
        {
            throw new ArgumentException("Commission rate cannot be negative.", nameof(rate));
        }

        if (minimum.Amount < 0)
        {
            throw new ArgumentException("Minimum commission cannot be negative.", nameof(minimum));
        }

        Rate = rate;
        Minimum = minimum;
    }

    public static Commission None => new(Percentage.Zero, Money.Zero);

    public static Commission FromPercentage(
        decimal percentageRate,
        decimal minimumAmount = 0m,
        string currency = "USD")
    {
        return new Commission(
            Percentage.FromPercentage(percentageRate),
            new Money(minimumAmount, currency));
    }

    public static Commission FromDecimal(
        decimal decimalRate,
        decimal minimumAmount = 0m,
        string currency = "USD")
    {
        return new Commission(
            Percentage.FromDecimal(decimalRate),
            new Money(minimumAmount, currency));
    }

    /// <summary>
    /// Calculates commission for a trade value.
    /// Takes the maximum of percentage-based commission and minimum commission.
    /// </summary>
    public Money CalculateFor(Money tradeValue)
    {
        if (tradeValue.Currency != Minimum.Currency)
        {
            throw new InvalidOperationException(
                $"Trade value currency ({tradeValue.Currency}) must match minimum commission currency ({Minimum.Currency})");
        }

        Money percentageCommission = Rate.Of(tradeValue);
        return Money.Max(percentageCommission, Minimum);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Rate;
        yield return Minimum;
    }

    public override string ToString()
    {
        if (Rate.Value == 0 && Minimum.Amount == 0)
        {
            return "No commission";
        }

        if (Minimum.Amount == 0)
        {
            return $"{Rate}";
        }

        if (Rate.Value == 0)
        {
            return $"Min: {Minimum}";
        }

        return $"{Rate}, Min: {Minimum}";
    }
}
