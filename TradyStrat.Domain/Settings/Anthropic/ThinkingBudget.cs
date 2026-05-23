using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Anthropic;

public readonly record struct ThinkingBudget
{
    public int Value { get; }

    private ThinkingBudget(int value) => Value = value;

    public static ThinkingBudget Of(int n)
    {
        if (n < 1024 || n > 16_000)
            throw new SettingValidationException($"Thinking budget must be in [1024, 16000], got {n}.");
        return new ThinkingBudget(n);
    }
}
