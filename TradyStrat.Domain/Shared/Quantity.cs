using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Shared;

public sealed class Quantity : ValueObject
{
    public decimal Value       { get; }
    public bool    IsSpecified { get; }

    private Quantity() { }   // EF
    private Quantity(decimal value, bool isSpecified) { Value = value; IsSpecified = isSpecified; }

    public static Quantity Of(decimal value)
    {
        if (value < 0m) throw new ArgumentException($"Quantity must be non-negative: {value}.", nameof(value));
        return new Quantity(value, isSpecified: true);
    }

    public static Quantity Zero => Of(0m);
    public static Quantity None { get; } = new(0m, isSpecified: false);

    public static Quantity operator +(Quantity a, Quantity b)
    {
        if (!a.IsSpecified || !b.IsSpecified) return None;
        return Of(a.Value + b.Value);
    }

    public static Quantity operator -(Quantity a, Quantity b)
    {
        if (!a.IsSpecified || !b.IsSpecified) return None;
        return Of(a.Value - b.Value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return IsSpecified;
    }

    public override string ToString()
        => IsSpecified ? Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "None";
}
