namespace TradyStrat.Domain.Settings.Tickers;

public readonly record struct FocusTicker
{
    public string Value { get; }

    private FocusTicker(string value) => Value = value;

    public static FocusTicker Of(string raw)
    {
        var t = (raw ?? "").Trim().ToUpperInvariant();
        if (t.Length == 0)
            throw new SettingValidationException("Focus ticker cannot be empty.");
        return new FocusTicker(t);
    }

    public override string ToString() => Value;
}
