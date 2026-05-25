namespace TradyStrat.Domain.Settings.Polymarket;

public readonly record struct MaxHorizonDays
{
    public int Value { get; }

    private MaxHorizonDays(int value) => Value = value;

    public static MaxHorizonDays Of(int n)
    {
        if (n < 1)
            throw new SettingValidationException($"Max horizon days must be >= 1, got {n}.");
        return new MaxHorizonDays(n);
    }
}
