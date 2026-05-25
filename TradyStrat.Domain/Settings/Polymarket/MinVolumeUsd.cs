namespace TradyStrat.Domain.Settings.Polymarket;

public readonly record struct MinVolumeUsd
{
    public decimal Value { get; }

    private MinVolumeUsd(decimal value) => Value = value;

    public static MinVolumeUsd Of(decimal n)
    {
        if (n < 0m)
            throw new SettingValidationException($"Min volume USD cannot be negative, got {n}.");
        return new MinVolumeUsd(n);
    }
}
