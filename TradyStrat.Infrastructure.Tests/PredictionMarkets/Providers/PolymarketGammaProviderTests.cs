using System.Net;
using Shouldly;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Infrastructure.PredictionMarkets.Providers;
using TradyStrat.Application.Settings;
using TradyStrat.Domain.Settings.Polymarket;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.PredictionMarkets.Providers;

public class PolymarketGammaProviderTests
{
    private const string FixtureRel = "PredictionMarkets/Fixtures/Polymarket";

    private static string ReadFixture(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, FixtureRel, name));

    private static (HttpClient http, StubHandler handler) BuildHttp(
        string baseUrl = "https://gamma-api.polymarket.com",
        Func<HttpRequestMessage, Task<HttpResponseMessage>>? respond = null)
    {
        var handler = new StubHandler(respond ?? (_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ReadFixture("gamma-markets-bitcoin.json")),
            })));
        var http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
        return (http, handler);
    }

    private sealed class StubPolymarketRepo(int maxMarkets, decimal minVolumeUsd, int maxHorizonDays, params string[] queries)
        : IPolymarketSettingsRepository
    {
        public Task<PolymarketSettings> GetAsync(CancellationToken ct)
            => Task.FromResult(new PolymarketSettings(
                SearchQueries.Of(queries),
                MaxMarkets.Of(maxMarkets),
                MinVolumeUsd.Of(minVolumeUsd),
                MaxHorizonDays.Of(maxHorizonDays)));

        public Task SaveAsync(PolymarketSettings settings, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<DateTime?> LastUpdatedAsync(CancellationToken ct)
            => Task.FromResult<DateTime?>(null);
    }

    private static StubPolymarketRepo Reader(int maxMarkets = 10)
        => new StubPolymarketRepo(maxMarkets, 0m, 3650, "bitcoin");   // wide horizon so fixtures aren't filtered out

    [Fact]
    public async Task Returns_normalized_filtered_list_on_success()
    {
        var (http, _) = BuildHttp();
        var sut = new PolymarketGammaProvider(http, Reader(), new FakeClock(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)));

        var result = await sut.GetMarketsAsync(TestContext.Current.CancellationToken);

        result.Count.ShouldBe(3);
        result[0].Slug.ShouldBe("btc-above-100k-eoy-2026");
    }

    [Fact]
    public async Task Throws_on_500()
    {
        var (http, _) = BuildHttp(respond: _ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var sut = new PolymarketGammaProvider(http, Reader(), new FakeClock(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)));

        await Should.ThrowAsync<PolymarketUnavailableException>(() =>
            sut.GetMarketsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Throws_on_unparseable_body()
    {
        var (http, _) = BuildHttp(respond: _ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ReadFixture("gamma-markets-malformed.json")),
            }));
        var sut = new PolymarketGammaProvider(http, Reader(), new FakeClock(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)));

        await Should.ThrowAsync<PolymarketUnavailableException>(() =>
            sut.GetMarketsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Empty_response_returns_empty_list_no_exception()
    {
        var (http, _) = BuildHttp(respond: _ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ReadFixture("gamma-markets-empty.json")),
            }));
        var sut = new PolymarketGammaProvider(http, Reader(), new FakeClock(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)));

        var result = await sut.GetMarketsAsync(TestContext.Current.CancellationToken);
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Issues_one_request_per_query_and_dedupes()
    {
        var reader = new StubPolymarketRepo(10, 0m, 3650, "bitcoin", "crypto");

        var requested = new List<string>();
        var (http, _) = BuildHttp(respond: req =>
        {
            lock (requested) requested.Add(req.RequestUri!.Query);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ReadFixture("gamma-markets-bitcoin.json")),
            });
        });
        var sut = new PolymarketGammaProvider(http, reader, new FakeClock(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)));

        var result = await sut.GetMarketsAsync(TestContext.Current.CancellationToken);

        requested.Count.ShouldBe(2);
        requested.ShouldContain(q => q.Contains("q=bitcoin"));
        requested.ShouldContain(q => q.Contains("q=crypto"));
        // Same fixture returned twice → 3 unique markets after dedup.
        result.Count.ShouldBe(3);
    }

    [Fact]
    public async Task One_query_failing_throws_and_discards_others()
    {
        var reader = new StubPolymarketRepo(10, 0m, 3650, "bitcoin", "crypto");

        var (http, _) = BuildHttp(respond: req =>
        {
            if (req.RequestUri!.Query.Contains("q=crypto"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ReadFixture("gamma-markets-bitcoin.json")),
            });
        });
        var sut = new PolymarketGammaProvider(http, reader, new FakeClock(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)));

        await Should.ThrowAsync<PolymarketUnavailableException>(() =>
            sut.GetMarketsAsync(TestContext.Current.CancellationToken));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => respond(request);
    }
}
