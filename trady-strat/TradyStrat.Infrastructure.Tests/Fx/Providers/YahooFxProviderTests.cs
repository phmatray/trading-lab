using TradyStrat.Infrastructure.Fx.Providers;
using System.Net;
using Shouldly;
using TradyStrat.Infrastructure.Tests.PriceFeed;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Fx.Providers;

public class YahooFxProviderTests
{
    [Fact]
    public async Task Returns_one_FxRate_per_day_with_close_as_Rate()
    {
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(File.ReadAllText("Fx/Fixtures/yahoo-eurusd-mini.json"))
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://query1.finance.yahoo.com") };
        var prov = new YahooFxProvider(http);

        var rates = await prov.FetchAsync("EUR", "USD", new(2024,4,30), new(2024,5,1),
            TestContext.Current.CancellationToken);

        rates.Count.ShouldBe(2);
        rates[0].Pair.Base.Code.ShouldBe("EUR");
        rates[0].Pair.Quote.Code.ShouldBe("USD");
        rates[0].Rate.ShouldBe(1.0820m);
        rates[1].Rate.ShouldBe(1.0835m);
    }
}
