namespace TradyStrat.Domain.Settings.Polymarket;

public readonly record struct MaxMarkets
{
    public int Value { get; }

    private MaxMarkets(int value) => Value = value;

    public static MaxMarkets Of(int n)
    {
        if (n < 1)
            throw new SettingValidationException($"Max markets must be >= 1, got {n}.");
        return new MaxMarkets(n);
    }
}
