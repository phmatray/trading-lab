using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Features.Fx;
using TradyStrat.Features.PriceFeed;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;
using Xunit;

namespace TradyStrat.Tests.PriceFeed;

public class PriceFeedHostedServiceTests
{
    [Fact]
    public async Task StartAsync_warms_all_three_tickers_and_eurusd()
    {
        await using var db = InMemoryDb.Create();
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));
        var feed  = new StubPriceFeed([]);
        var fx    = new StubFxProvider([]);

        var services = new ServiceCollection();
        services.AddScoped<DailyPriceCache>(_ =>
            new DailyPriceCache(feed, db, clock, NullLogger<DailyPriceCache>.Instance));
        services.AddScoped<DailyFxCache>(_ =>
            new DailyFxCache(fx, db, clock, NullLogger<DailyFxCache>.Instance));

        var sp = services.BuildServiceProvider();
        var svc = new PriceFeedHostedService(sp, NullLogger<PriceFeedHostedService>.Instance);

        await svc.StartAsync(TestContext.Current.CancellationToken);

        feed.CallCount.ShouldBe(3);    // CON3.DE, COIN, BTC-USD
        fx.CallCount.ShouldBe(1);      // EURUSD
    }
}
