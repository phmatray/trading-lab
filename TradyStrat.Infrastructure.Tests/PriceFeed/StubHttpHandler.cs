namespace TradyStrat.Infrastructure.Tests.PriceFeed;

public sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    : HttpMessageHandler
{
    public List<HttpRequestMessage> Calls { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls.Add(request);
        return Task.FromResult(respond(request));
    }
}
