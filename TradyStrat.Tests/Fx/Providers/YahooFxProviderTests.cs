using System.Net;
using TradyStrat.Features.Fx.Providers;
using Shouldly;
using TradyStrat.Tests.PriceFeed;
using Xunit;

namespace TradyStrat.Tests.Fx.Providers;

public class YahooFxProviderTests
{
    [Fact]
    public async Task Returns_one_FxRate_per_day_with_close_as_UsdPerEur()
    {
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(File.ReadAllText("Fx/Fixtures/yahoo-eurusd-mini.json"))
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://query1.finance.yahoo.com") };
        var prov = new YahooFxProvider(http);

        var rates = await prov.FetchAsync("EURUSD", new(2024,4,30), new(2024,5,1),
            TestContext.Current.CancellationToken);

        rates.Count.ShouldBe(2);
        rates[0].Pair.ShouldBe("EURUSD");
        rates[0].UsdPerEur.ShouldBe(1.0820m);
        rates[1].UsdPerEur.ShouldBe(1.0835m);
    }
}
