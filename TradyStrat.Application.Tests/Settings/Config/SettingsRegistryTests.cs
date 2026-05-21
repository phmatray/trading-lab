using Shouldly;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Application.Settings.Config;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Settings.Config;

public class SettingsRegistryTests
{
    private static readonly string[] ExpectedKeys =
    [
        "anthropic.model", "anthropic.maxTokens", "anthropic.thinkingBudget",
        "anthropic.maxParallelSuggestions",
        "polymarket.searchQueries", "polymarket.maxMarkets",
        "polymarket.minVolumeUsd", "polymarket.maxHorizonDays",
        "tickers.focus",
    ];

    private static readonly string[] BlankQueryArray = ["bitcoin", " "];
    private static readonly string[] ValidQueryArray = ["bitcoin", "fed"];

    [Fact]
    public void Contains_exactly_the_expected_keys()
    {
        var registry = new SettingsRegistry();
        registry.All.Keys.OrderBy(k => k).ShouldBe(ExpectedKeys.OrderBy(k => k));
    }

    [Fact]
    public void Every_default_round_trips_through_parse_and_format()
    {
        var registry = new SettingsRegistry();
        foreach (var d in registry.All.Values)
        {
            var parsed = Should.NotThrow(() => d.Parse(d.DefaultRaw));
            d.Validate?.Invoke(parsed);                              // default must be valid
            if (d.Format is not null)
            {
                var reformatted = d.Format(parsed);
                Should.NotThrow(() => d.Parse(reformatted));        // re-parsing the reformatted value still works
            }
        }
    }

    [Fact]
    public void Get_unknown_key_throws_InvalidOperationException()
    {
        var registry = new SettingsRegistry();
        Should.Throw<InvalidOperationException>(() => registry.Get("does.not.exist"));
    }

    [Fact]
    public void MaxTokens_validate_rejects_zero_and_huge()
    {
        var registry = new SettingsRegistry();
        var d = registry.Get("anthropic.maxTokens");
        Should.Throw<SettingValidationException>(() => d.Validate!.Invoke(0));
        Should.Throw<SettingValidationException>(() => d.Validate!.Invoke(100_001));
        Should.NotThrow(() => d.Validate!.Invoke(1500));
    }

    [Fact]
    public void SearchQueries_validate_rejects_empty_and_blank_entries()
    {
        var registry = new SettingsRegistry();
        var d = registry.Get("polymarket.searchQueries");
        Should.Throw<SettingValidationException>(() => d.Validate!.Invoke(Array.Empty<string>()));
        Should.Throw<SettingValidationException>(() => d.Validate!.Invoke(BlankQueryArray));
        Should.NotThrow(() => d.Validate!.Invoke(ValidQueryArray));
    }
}
