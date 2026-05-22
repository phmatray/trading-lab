namespace TradyStrat.Domain.Shared;

public readonly record struct Currency
{
    public string Code { get; }

    private Currency(string code) => Code = code;

    public static Currency Parse(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 3)
            throw new ArgumentException($"Invalid ISO 4217 code: '{code}'.", nameof(code));
        return new Currency(code.Trim().ToUpperInvariant());
    }

    public static Currency Eur => new("EUR");
    public static Currency Usd => new("USD");
    public static Currency Gbp => new("GBP");

    public override string ToString() => Code;
}
