using System.Net;
using System.Text;
using System.Text.Json;

namespace TradingSignal.Llm.Tests.Fakes;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responders = new();

    public List<HttpRequestMessage> ReceivedRequests { get; } = new();
    public List<JsonElement> ReceivedBodies { get; } = new();

    public void EnqueueJson(object body, HttpStatusCode status = HttpStatusCode.OK)
        => _responders.Enqueue(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        });

    public void EnqueueRawJson(string rawJson, HttpStatusCode status = HttpStatusCode.OK)
        => _responders.Enqueue(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(rawJson, Encoding.UTF8, "application/json"),
        });

    public void EnqueueException(Exception ex)
        => _responders.Enqueue(_ => throw ex);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ReceivedRequests.Add(request);
        if (request.Content is not null)
        {
            string body = await request.Content.ReadAsStringAsync(cancellationToken);
            ReceivedBodies.Add(JsonDocument.Parse(body).RootElement.Clone());
        }
        if (_responders.Count == 0)
            throw new InvalidOperationException("FakeHttpMessageHandler: no responses queued");
        var responder = _responders.Dequeue();
        return responder(request);
    }
}
