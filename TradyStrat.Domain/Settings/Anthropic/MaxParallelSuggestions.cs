using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Anthropic;

public readonly record struct MaxParallelSuggestions(int Value)
{
    public static MaxParallelSuggestions Of(int n)
    {
        if (n < 1 || n > 10)
            throw new SettingValidationException($"Max parallel suggestions must be in [1, 10], got {n}.");
        return new MaxParallelSuggestions(n);
    }
}
