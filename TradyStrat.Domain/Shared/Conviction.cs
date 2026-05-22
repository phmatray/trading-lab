using System.Globalization;

namespace TradyStrat.Domain.Shared;

public readonly record struct Conviction
{
    public int Value { get; }

    private Conviction(int value) => Value = value;

    public static Conviction Of(int value)
    {
        if (value < 1 || value > 10)
            throw new ArgumentException($"Conviction must be in [1..10]: {value}.", nameof(value));
        return new Conviction(value);
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
