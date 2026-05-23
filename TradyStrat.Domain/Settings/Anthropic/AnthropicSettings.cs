namespace TradyStrat.Domain.Settings.Anthropic;

public sealed record AnthropicSettings(
    AnthropicModel Model,
    MaxTokens MaxTokens,
    ThinkingBudget ThinkingBudget,
    MaxParallelSuggestions MaxParallelSuggestions);
