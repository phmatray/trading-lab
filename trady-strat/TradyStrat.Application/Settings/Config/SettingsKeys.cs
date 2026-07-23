namespace TradyStrat.Application.Settings.Config;

public static class SettingsKeys
{
    public const string AnthropicModel                  = "anthropic.model";
    public const string AnthropicMaxTokens              = "anthropic.maxTokens";
    public const string AnthropicThinkingBudget         = "anthropic.thinkingBudget";
    public const string AnthropicMaxParallelSuggestions = "anthropic.maxParallelSuggestions";
    public const string PolymarketSearchQueries         = "polymarket.searchQueries";
    public const string PolymarketMaxMarkets            = "polymarket.maxMarkets";
    public const string PolymarketMinVolumeUsd          = "polymarket.minVolumeUsd";
    public const string PolymarketMaxHorizonDays        = "polymarket.maxHorizonDays";
    public const string TickersFocus                    = "tickers.focus";
}
