using Shouldly;
using TradyStrat.Application.PredictionMarkets;
using Xunit;

namespace TradyStrat.Application.Tests.PredictionMarkets;

public class PolymarketFilterTests
{
    private static PredictionMarket M(string slug, decimal volume, DateOnly endDate) =>
        new(Slug: slug, Question: slug, Probability: 0.5m,
            EndDate: endDate, VolumeUsd: volume, Tags: ["bitcoin"]);

    [Fact]
    public void Dedupes_by_slug_keeping_first()
    {
        var input = new[]
        {
            M("a", 100m, new DateOnly(2026, 12, 31)),
            M("a", 999m, new DateOnly(2026, 12, 31)),  // duplicate slug
            M("b", 200m, new DateOnly(2026, 12, 31)),
        };
        var result = PolymarketFilter.Apply(input,
            today: new DateOnly(2026, 5, 7),
            minVolumeUsd: 0m,
            maxHorizonDays: 365,
            maxMarkets: 10);

        result.Select(m => m.Slug).ShouldBe(["b", "a"]); // ordered by volume desc
        result.Single(m => m.Slug == "a").VolumeUsd.ShouldBe(100m);
    }

    [Fact]
    public void Filters_below_min_volume()
    {
        var input = new[]
        {
            M("a", 1000m,  new DateOnly(2026, 12, 31)),
            M("b", 50_000m, new DateOnly(2026, 12, 31)),
        };
        var result = PolymarketFilter.Apply(input,
            today: new DateOnly(2026, 5, 7),
            minVolumeUsd: 10_000m,
            maxHorizonDays: 365,
            maxMarkets: 10);

        result.Select(m => m.Slug).ShouldBe(["b"]);
    }

    [Fact]
    public void Filters_beyond_horizon()
    {
        var input = new[]
        {
            M("near", 100m, new DateOnly(2026, 6, 1)),
            M("far",  100m, new DateOnly(2030, 1, 1)),
        };
        var result = PolymarketFilter.Apply(input,
            today: new DateOnly(2026, 5, 7),
            minVolumeUsd: 0m,
            maxHorizonDays: 30,
            maxMarkets: 10);

        result.Select(m => m.Slug).ShouldBe(["near"]);
    }

    [Fact]
    public void Orders_by_volume_descending_and_takes_max()
    {
        var input = new[]
        {
            M("low",  100m, new DateOnly(2026, 12, 31)),
            M("high", 999m, new DateOnly(2026, 12, 31)),
            M("mid",  500m, new DateOnly(2026, 12, 31)),
        };
        var result = PolymarketFilter.Apply(input,
            today: new DateOnly(2026, 5, 7),
            minVolumeUsd: 0m,
            maxHorizonDays: 365,
            maxMarkets: 2);

        result.Select(m => m.Slug).ShouldBe(["high", "mid"]);
    }
}
