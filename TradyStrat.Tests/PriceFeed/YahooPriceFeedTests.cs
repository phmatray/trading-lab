using System.Net;
using Shouldly;
using TradyStrat.Features.PriceFeed;
using TradyStrat.Shared.Exceptions;
using Xunit;

namespace TradyStrat.Tests.PriceFeed;

public class YahooPriceFeedTests
{
    private static HttpClient ClientReturning(HttpStatusCode code, string body)
    {
        var handler = new StubHttpHandler(_ =>
            new HttpResponseMessage(code) { Content = new StringContent(body) });
        return new HttpClient(handler) { BaseAddress = new Uri("https://query1.finance.yahoo.com") };
    }

    [Fact]
    public async Task Builds_url_with_unix_timestamps_and_daily_interval()
    {
        var captured = new List<HttpRequestMessage>();
        var handler = new StubHttpHandler(req =>
        {
            captured.Add(req);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText("PriceFeed/Fixtures/yahoo-con3-mini.json"))
            };
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://query1.finance.yahoo.com") };
        var feed = new YahooPriceFeed(http);

        var ct = TestContext.Current.CancellationToken;
        var bars = await feed.FetchDailyAsync("CON3.DE", new(2024,4,30), new(2024,5,2), ct);

        bars.Count.ShouldBe(3);
        captured[0].RequestUri!.PathAndQuery.ShouldContain("/v8/finance/chart/CON3.DE");
        captured[0].RequestUri!.PathAndQuery.ShouldContain("interval=1d");
    }

    [Fact]
    public async Task Throws_PriceFeedUnavailableException_on_5xx()
    {
        var feed = new YahooPriceFeed(ClientReturning(HttpStatusCode.InternalServerError, ""));

        await Should.ThrowAsync<PriceFeedUnavailableException>(() =>
            feed.FetchDailyAsync("X", new(2024,1,1), new(2024,1,2),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Throws_PriceFeedUnavailableException_on_invalid_json()
    {
        var feed = new YahooPriceFeed(ClientReturning(HttpStatusCode.OK, "not json"));

        await Should.ThrowAsync<PriceFeedUnavailableException>(() =>
            feed.FetchDailyAsync("X", new(2024,1,1), new(2024,1,2),
                TestContext.Current.CancellationToken));
    }
}
