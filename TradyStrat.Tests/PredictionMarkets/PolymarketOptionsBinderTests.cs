using Microsoft.Extensions.Configuration;
using Shouldly;
using TradyStrat.Features.PredictionMarkets;
using Xunit;

namespace TradyStrat.Tests.PredictionMarkets;

public class PolymarketOptionsBinderTests
{
    [Fact]
    public void Returns_defaults_when_section_missing()
    {
        var cfg = new ConfigurationBuilder().Build();   // no Polymarket section
        var opts = PolymarketOptionsBinder.Read(cfg);

        opts.BaseUrl.ShouldBe("https://gamma-api.polymarket.com");
        opts.SearchQueries.ShouldBe(["bitcoin", "ethereum", "coinbase", "fed"]);
        opts.MaxMarkets.ShouldBe(10);
        opts.MinVolumeUsd.ShouldBe(50_000m);
        opts.MaxHorizonDays.ShouldBe(365);
    }

    [Fact]
    public void Reads_overrides_from_configuration()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Polymarket:BaseUrl"]         = "https://example.test",
                ["Polymarket:SearchQueries:0"] = "ethereum",
                ["Polymarket:MaxMarkets"]      = "5",
                ["Polymarket:MinVolumeUsd"]   = "1000",
                ["Polymarket:MaxHorizonDays"] = "90",
            })
            .Build();
        var opts = PolymarketOptionsBinder.Read(cfg);

        opts.BaseUrl.ShouldBe("https://example.test");
        opts.SearchQueries.ShouldBe(["ethereum"]);
        opts.MaxMarkets.ShouldBe(5);
        opts.MinVolumeUsd.ShouldBe(1000m);
        opts.MaxHorizonDays.ShouldBe(90);
    }

    [Fact]
    public void Falls_back_to_legacy_Tags_when_SearchQueries_missing()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Polymarket:Tags:0"] = "bitcoin",
                ["Polymarket:Tags:1"] = "ethereum",
            })
            .Build();
        var opts = PolymarketOptionsBinder.Read(cfg);

        opts.SearchQueries.ShouldBe(["bitcoin", "ethereum"]);
    }

    [Theory]
    [InlineData("MaxMarkets",     "0")]
    [InlineData("MaxMarkets",     "-1")]
    [InlineData("MinVolumeUsd",   "-1")]
    [InlineData("MaxHorizonDays", "0")]
    public void Throws_on_invalid_config(string key, string value)
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Polymarket:{key}"] = value,
            })
            .Build();
        Should.Throw<ArgumentOutOfRangeException>(() => PolymarketOptionsBinder.Read(cfg));
    }
}
