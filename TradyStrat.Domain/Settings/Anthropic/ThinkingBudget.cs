using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Anthropic;

public readonly record struct ThinkingBudget(int Value)
{
    public static ThinkingBudget Of(int n)
    {
        if (n < 1024 || n > 16_000)
            throw new SettingValidationException($"Thinking budget must be in [1024, 16000], got {n}.");
        return new ThinkingBudget(n);
    }
}
