namespace TradyStrat.Application.Settings.Config;

public sealed record AnthropicSettings(string Model, int MaxTokens, int ThinkingBudget);

public sealed record PolymarketSettings(
    IReadOnlyList<string> SearchQueries,
    int MaxMarkets,
    decimal MinVolumeUsd,
    int MaxHorizonDays);
