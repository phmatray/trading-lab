using System.Globalization;
using System.Text.Json;
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Application.Settings.Config;

/// <summary>
/// Encapsulates everything per-setting: its default raw value, how to parse the
/// stored string into a typed value, how to validate that value, and how to format
/// it back to canonical text. The registry of these is the single source of truth —
/// the seeder, ISettingsService.GetAsync&lt;T&gt;, and UpdateSettingUseCase all
/// dispatch through it instead of switching on the key.
/// </summary>
public sealed class SettingDescriptor
{
    public required string Key { get; init; }
    public required string DefaultRaw { get; init; }
    public required Func<string, object> Parse { get; init; }
    public Action<object>? Validate { get; init; }
    public Func<object, string>? Format { get; init; }
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
            new()
            {
                Key = SettingsKeys.AnthropicModel,
                DefaultRaw = "claude-opus-4-7",
                Parse = s => s.Trim(),                      // trimmed so a stray-whitespace model id can't reach the API
                Validate = v => RequireNonEmpty((string)v, "Model"),
                Format = v => (string)v,                    // store the trimmed value
            },
            new()
            {
                Key = SettingsKeys.AnthropicMaxTokens,
                DefaultRaw = "1500",
                Parse = s => int.Parse(s, CultureInfo.InvariantCulture),
                Validate = v => RequireRange((int)v, 1, 100_000, "Max tokens"),
                Format = v => ((int)v).ToString(CultureInfo.InvariantCulture),
            },
            new()
            {
                Key = SettingsKeys.AnthropicThinkingBudget,
                DefaultRaw = "8192",
                Parse = s => int.Parse(s, CultureInfo.InvariantCulture),
                Validate = v => RequireRange((int)v, 1024, 16_000, "Thinking budget"),
                Format = v => ((int)v).ToString(CultureInfo.InvariantCulture),
            },
            new()
            {
                Key = SettingsKeys.PolymarketSearchQueries,
                DefaultRaw = """["bitcoin","ethereum","coinbase","fed"]""",
                Parse = s => JsonSerializer.Deserialize<string[]>(s)
                             ?? throw new FormatException("Search queries must be a JSON array of strings."),
                Validate = v =>
                {
                    var arr = (string[])v;
                    if (arr.Length == 0)
                        throw new SettingValidationException("At least one search query is required.");
                    if (arr.Any(string.IsNullOrWhiteSpace))
                        throw new SettingValidationException("Search queries cannot be blank.");
                },
                Format = v => JsonSerializer.Serialize((string[])v),
            },
            new()
            {
                Key = SettingsKeys.PolymarketMaxMarkets,
                DefaultRaw = "8",
                Parse = s => int.Parse(s, CultureInfo.InvariantCulture),
                Validate = v => RequireAtLeast((int)v, 1, "Max markets"),
                Format = v => ((int)v).ToString(CultureInfo.InvariantCulture),
            },
            new()
            {
                Key = SettingsKeys.PolymarketMinVolumeUsd,
                DefaultRaw = "50000",
                Parse = s => decimal.Parse(s, CultureInfo.InvariantCulture),
                Validate = v =>
                {
                    if ((decimal)v < 0m)
                        throw new SettingValidationException("Min volume USD cannot be negative.");
                },
                Format = v => ((decimal)v).ToString(CultureInfo.InvariantCulture),
            },
            new()
            {
                Key = SettingsKeys.PolymarketMaxHorizonDays,
                DefaultRaw = "365",
                Parse = s => int.Parse(s, CultureInfo.InvariantCulture),
                Validate = v => RequireAtLeast((int)v, 1, "Max horizon days"),
                Format = v => ((int)v).ToString(CultureInfo.InvariantCulture),
            },
            new()
            {
                Key = SettingsKeys.TickersFocus,
                DefaultRaw = "CON3.L",
                Parse = s => s.Trim(),                       // defensive: a stray space shouldn't reach instrument lookup
                Validate = v => RequireNonEmpty((string)v, "Focus ticker"),
                Format = v => (string)v,                     // store the trimmed value
            },
        ];
        return list.ToDictionary(d => d.Key);
    }

    private static void RequireNonEmpty(string s, string name)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new SettingValidationException($"{name} cannot be empty.");
    }

    private static void RequireRange(int n, int min, int max, string name)
    {
        if (n < min || n > max)
            throw new SettingValidationException($"{name} must be between {min} and {max}.");
    }

    private static void RequireAtLeast(int n, int min, string name)
    {
        if (n < min)
            throw new SettingValidationException($"{name} must be at least {min}.");
    }
}
