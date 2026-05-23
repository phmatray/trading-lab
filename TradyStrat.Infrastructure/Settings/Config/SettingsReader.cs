using TradyStrat.Application.Settings.Config;
using TradyStrat.Domain.Settings.Anthropic;
using TradyStrat.Domain.Settings.Polymarket;
namespace TradyStrat.Infrastructure.Settings.Config;

public sealed class SettingsReader(ISettingsService settings) : ISettingsReader
{
    public async Task<AnthropicSettings> AnthropicAsync(CancellationToken ct) => new(
        Model:                  AnthropicModel.Of(await settings.GetAsync<string>(SettingsKeys.AnthropicModel, ct)),
        MaxTokens:              MaxTokens.Of(await settings.GetAsync<int>(SettingsKeys.AnthropicMaxTokens, ct)),
        ThinkingBudget:         ThinkingBudget.Of(await settings.GetAsync<int>(SettingsKeys.AnthropicThinkingBudget, ct)),
        MaxParallelSuggestions: MaxParallelSuggestions.Of(await settings.GetAsync<int>(SettingsKeys.AnthropicMaxParallelSuggestions, ct)));

    public async Task<PolymarketSettings> PolymarketAsync(CancellationToken ct) => new(
        SearchQueries:  SearchQueries.Of(await settings.GetAsync<string[]>(SettingsKeys.PolymarketSearchQueries, ct)),
        MaxMarkets:     MaxMarkets.Of(await settings.GetAsync<int>(SettingsKeys.PolymarketMaxMarkets, ct)),
        MinVolumeUsd:   MinVolumeUsd.Of(await settings.GetAsync<decimal>(SettingsKeys.PolymarketMinVolumeUsd, ct)),
        MaxHorizonDays: MaxHorizonDays.Of(await settings.GetAsync<int>(SettingsKeys.PolymarketMaxHorizonDays, ct)));

    public Task<string> FocusTickerAsync(CancellationToken ct)
        => settings.GetRawAsync(SettingsKeys.TickersFocus, ct);

    public Task<DateTime?> LastUpdatedAsync(IEnumerable<string> keys, CancellationToken ct)
        => settings.LastUpdatedAsync(keys, ct);
}
