using Shouldly;
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

    [Fact]
    public void Contains_exactly_the_expected_keys()
    {
        var registry = new SettingsRegistry();
        registry.All.Keys.OrderBy(k => k).ShouldBe(ExpectedKeys.OrderBy(k => k));
    }

    [Fact]
    public void Every_descriptor_has_a_non_empty_default()
    {
        var registry = new SettingsRegistry();
        foreach (var d in registry.All.Values)
        {
            d.DefaultRaw.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Get_unknown_key_throws_InvalidOperationException()
    {
        var registry = new SettingsRegistry();
        Should.Throw<InvalidOperationException>(() => registry.Get("does.not.exist"));
    }
}
