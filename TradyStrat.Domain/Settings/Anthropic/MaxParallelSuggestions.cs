using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Anthropic;

public readonly record struct MaxParallelSuggestions
{
    public int Value { get; }

    private MaxParallelSuggestions(int value) => Value = value;

    public static MaxParallelSuggestions Of(int n)
    {
        if (n < 1 || n > 10)
            throw new SettingValidationException($"Max parallel suggestions must be in [1, 10], got {n}.");
        return new MaxParallelSuggestions(n);
    }
}
