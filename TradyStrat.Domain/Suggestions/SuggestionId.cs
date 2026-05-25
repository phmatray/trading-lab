namespace TradyStrat.Domain.Suggestions;

public readonly record struct SuggestionId(int Value)
{
    public static SuggestionId New() => new(0);
    public override string ToString() => $"SuggestionId({Value})";
}
