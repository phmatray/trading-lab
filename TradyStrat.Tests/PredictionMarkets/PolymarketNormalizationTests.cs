using System.Text.Json;
using Shouldly;
using TradyStrat.Infrastructure.PredictionMarkets.Providers;
using Xunit;

namespace TradyStrat.Tests.PredictionMarkets;

// Pure parsing logic. Inline JSON keeps these tests independent of the API
// envelope shape (the public-search `{events: [{markets: [...]}]}` wrapping
// is the provider's concern; this layer is given a flat market array).
public class PolymarketNormalizationTests
{
    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public void Parses_three_binary_btc_markets()
    {
        var arr = Parse("""
            [
              {
                "slug": "btc-above-100k-eoy-2026",
                "question": "Will Bitcoin close above $100,000 on Dec 31, 2026?",
                "outcomes": "[\"Yes\", \"No\"]",
                "outcomePrices": "[\"0.32\", \"0.68\"]",
                "endDate": "2026-12-31T00:00:00Z",
                "volume": "1250000",
                "tags": [{"slug": "bitcoin"}, {"slug": "crypto"}]
              },
              {
                "slug": "btc-above-80k-eoy-2026",
                "question": "Will Bitcoin close above $80,000 on Dec 31, 2026?",
                "outcomes": "[\"Yes\", \"No\"]",
                "outcomePrices": "[\"0.71\", \"0.29\"]",
                "endDate": "2026-12-31T00:00:00Z",
                "volume": "780000",
                "tags": [{"slug": "bitcoin"}]
              },
              {
                "slug": "btc-below-50k-q3-2026",
                "question": "Will Bitcoin trade below $50,000 at any point in Q3 2026?",
                "outcomes": "[\"Yes\", \"No\"]",
                "outcomePrices": "[\"0.12\", \"0.88\"]",
                "endDate": "2026-09-30T00:00:00Z",
                "volume": "320000",
                "tags": [{"slug": "bitcoin"}]
              }
            ]
            """);
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
        var arr = Parse("""
            [
              {
                "slug": "coin-beats-q3-2026",
                "question": "Will Coinbase beat Q3 2026 EPS estimates?",
                "outcomes": "[\"Yes\", \"No\"]",
                "outcomePrices": "[\"0.58\", \"0.42\"]",
                "endDate": "2026-11-15T00:00:00Z",
                "volume": "450000",
                "tags": [{"slug": "coinbase"}]
              },
              {
                "slug": "next-fomc-decision",
                "question": "Next FOMC rate decision",
                "outcomes": "[\"Cut 25bp\", \"Cut 50bp\", \"Hold\", \"Hike\"]",
                "outcomePrices": "[\"0.40\", \"0.10\", \"0.45\", \"0.05\"]",
                "endDate": "2026-09-17T00:00:00Z",
                "volume": "2000000",
                "tags": [{"slug": "fed"}]
              },
              {
                "slug": "weird-market",
                "question": "Will a meteor strike?",
                "outcomes": "[\"Maybe\"]",
                "outcomePrices": "[\"0.50\"]",
                "endDate": "2099-01-01T00:00:00Z",
                "volume": "100",
                "tags": []
              }
            ]
            """);
        var markets = PolymarketNormalizer.Normalize(arr).ToList();

        markets.Count.ShouldBe(1);                       // only "coin-beats-q3-2026" survives
        markets[0].Slug.ShouldBe("coin-beats-q3-2026");
    }

    [Fact]
    public void Returns_empty_for_empty_input()
    {
        var arr = Parse("[]");
        PolymarketNormalizer.Normalize(arr).ShouldBeEmpty();
    }
}
