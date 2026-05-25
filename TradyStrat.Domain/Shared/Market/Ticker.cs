namespace TradyStrat.Domain.Shared.Market;

public readonly record struct Ticker
{
    public string Value { get; }

    private Ticker(string value) => Value = value;

    public static Ticker Of(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Any(char.IsWhiteSpace))
            throw new ArgumentException($"Invalid ticker: '{symbol}'.", nameof(symbol));
        return new Ticker(symbol.Trim());
    }

    public override string ToString() => Value;
}
