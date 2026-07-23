namespace TradyStrat.Domain.Shared.Market;

public readonly record struct Exchange
{
    public string Code { get; }

    private Exchange(string code) => Code = code;

    public static Exchange Of(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Exchange code must not be empty.", nameof(code));
        return new Exchange(code.Trim());
    }

    public override string ToString() => Code;
}
