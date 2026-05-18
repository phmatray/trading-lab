namespace TradyStrat.Application.Settings.Config;

public sealed record AnthropicSettings(string Model, int MaxTokens);

public sealed record PolymarketSettings(
    IReadOnlyList<string> SearchQueries,
    int MaxMarkets,
    decimal MinVolumeUsd,
    int MaxHorizonDays);
