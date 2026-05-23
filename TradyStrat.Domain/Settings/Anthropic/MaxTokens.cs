using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Anthropic;

public readonly record struct MaxTokens
{
    public int Value { get; }

    private MaxTokens(int value) => Value = value;

    public static MaxTokens Of(int n)
    {
        if (n < 1 || n > 100_000)
            throw new SettingValidationException($"Max tokens must be in [1, 100000], got {n}.");
        return new MaxTokens(n);
    }
}
