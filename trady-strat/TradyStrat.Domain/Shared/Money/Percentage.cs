namespace TradyStrat.Domain.Shared.Money;

public readonly record struct Percentage
{
    public decimal Value   { get; }
    public bool    IsEmpty { get; }

    private Percentage(decimal value, bool isEmpty)
    {
        Value = value;
        IsEmpty = isEmpty;
    }

    public static Percentage Of(decimal value) => new(value, isEmpty: false);
    public static Percentage Empty { get; } = new(0m, isEmpty: true);

    public override string ToString()
        => IsEmpty ? "—" : Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "%";
}
