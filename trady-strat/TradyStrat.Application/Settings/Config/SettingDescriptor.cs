namespace TradyStrat.Application.Settings.Config;

/// <summary>
/// Minimal per-setting descriptor: the registry key and its seed default. The seeder is the only
/// consumer — typed repositories (EfAnthropicSettingsRepository etc.) own parsing/validation/formatting.
/// </summary>
public sealed class SettingDescriptor
{
    public required string Key { get; init; }
    public required string DefaultRaw { get; init; }
}

/// <summary>The set of <see cref="SettingDescriptor"/>s keyed by setting key — the single source of truth downstream consumers dispatch through.</summary>
public interface ISettingsRegistry
{
    IReadOnlyDictionary<string, SettingDescriptor> All { get; }

    /// <summary>The descriptor for <paramref name="key"/>; throws <see cref="InvalidOperationException"/> if the key is unknown (= bug).</summary>
    SettingDescriptor Get(string key);
}

public sealed class SettingsRegistry : ISettingsRegistry
{
    public IReadOnlyDictionary<string, SettingDescriptor> All { get; } = Build();

    public SettingDescriptor Get(string key) =>
        All.TryGetValue(key, out var d)
            ? d
            : throw new InvalidOperationException($"Unknown setting key '{key}'.");

    private static Dictionary<string, SettingDescriptor> Build()
    {
        SettingDescriptor[] list =
        [
            new() { Key = SettingsKeys.AnthropicModel,                  DefaultRaw = "claude-opus-4-7" },
            new() { Key = SettingsKeys.AnthropicMaxTokens,              DefaultRaw = "1500" },
            new() { Key = SettingsKeys.AnthropicThinkingBudget,         DefaultRaw = "8192" },
            new() { Key = SettingsKeys.AnthropicMaxParallelSuggestions, DefaultRaw = "3" },
            new() { Key = SettingsKeys.PolymarketSearchQueries,         DefaultRaw = """["bitcoin","ethereum","coinbase","fed"]""" },
            new() { Key = SettingsKeys.PolymarketMaxMarkets,            DefaultRaw = "8" },
            new() { Key = SettingsKeys.PolymarketMinVolumeUsd,          DefaultRaw = "50000" },
            new() { Key = SettingsKeys.PolymarketMaxHorizonDays,        DefaultRaw = "365" },
            new() { Key = SettingsKeys.TickersFocus,                    DefaultRaw = "CON3.L" },
        ];
        return list.ToDictionary(d => d.Key);
    }
}
