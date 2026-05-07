using System.Text.Json;
using Shouldly;
using TradyStrat.Features.PredictionMarkets;
using TradyStrat.Features.PredictionMarkets.Providers;
using Xunit;

namespace TradyStrat.Tests.PredictionMarkets;

public class PolymarketNormalizationTests
{
    private static JsonElement Load(string fixtureName)
    {
        var path = Path.Combine(AppContext.BaseDirectory,
            "PredictionMarkets", "Fixtures", "Polymarket", fixtureName);
        var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.Clone();
    }

    [Fact]
    public void Parses_three_binary_btc_markets()
    {
        var arr = Load("gamma-markets-bitcoin.json");
        var markets = PolymarketNormalizer.Normalize(arr).ToList();

        markets.Count.ShouldBe(3);
        markets[0].Slug.ShouldBe("btc-above-100k-eoy-2026");
        markets[0].Probability.ShouldBe(0.32m);
        markets[0].Tags.ShouldContain("bitcoin");
        markets[0].VolumeUsd.ShouldBe(1_250_000m);
        markets[0].EndDate.ShouldBe(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public void Drops_multi_outcome_and_malformed_markets()
    {
        var arr = Load("gamma-markets-multi-outcome.json");
        var markets = PolymarketNormalizer.Normalize(arr).ToList();

        markets.Count.ShouldBe(1);                       // only "coin-beats-q3-2026" survives
        markets[0].Slug.ShouldBe("coin-beats-q3-2026");
    }

    [Fact]
    public void Returns_empty_for_empty_input()
    {
        var arr = Load("gamma-markets-empty.json");
        PolymarketNormalizer.Normalize(arr).ShouldBeEmpty();
    }
}
