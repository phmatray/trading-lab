using TradyStrat.Application.Settings.Config;
namespace TradyStrat.Infrastructure.Settings.Config;

public sealed class SettingsReader(ISettingsService settings) : ISettingsReader
{
    public async Task<AnthropicSettings> AnthropicAsync(CancellationToken ct) => new(
        Model:                  await settings.GetAsync<string>(SettingsKeys.AnthropicModel, ct),
        MaxTokens:              await settings.GetAsync<int>(SettingsKeys.AnthropicMaxTokens, ct),
        ThinkingBudget:         await settings.GetAsync<int>(SettingsKeys.AnthropicThinkingBudget, ct),
        MaxParallelSuggestions: await settings.GetAsync<int>(SettingsKeys.AnthropicMaxParallelSuggestions, ct));

    public async Task<PolymarketSettings> PolymarketAsync(CancellationToken ct) => new(
        SearchQueries:  await settings.GetAsync<string[]>(SettingsKeys.PolymarketSearchQueries, ct),
        MaxMarkets:     await settings.GetAsync<int>(SettingsKeys.PolymarketMaxMarkets, ct),
        MinVolumeUsd:   await settings.GetAsync<decimal>(SettingsKeys.PolymarketMinVolumeUsd, ct),
        MaxHorizonDays: await settings.GetAsync<int>(SettingsKeys.PolymarketMaxHorizonDays, ct));

    public Task<string> FocusTickerAsync(CancellationToken ct)
        => settings.GetRawAsync(SettingsKeys.TickersFocus, ct);

    public Task<DateTime?> LastUpdatedAsync(IEnumerable<string> keys, CancellationToken ct)
        => settings.LastUpdatedAsync(keys, ct);
}
