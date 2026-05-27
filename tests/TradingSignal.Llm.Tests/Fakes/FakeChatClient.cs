using Microsoft.Extensions.AI;

namespace TradingSignal.Llm.Tests.Fakes;

internal sealed class FakeChatClient : IChatClient
{
    private readonly Queue<Func<ChatResponse>> _responders = new();
    public int CallCount { get; private set; }
    public List<ChatOptions?> ReceivedOptions { get; } = new();

    public void EnqueueText(string text)
        => _responders.Enqueue(() => new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));

    public void EnqueueError(Exception ex)
        => _responders.Enqueue(() => throw ex);

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        CallCount++;
        ReceivedOptions.Add(options);
        if (_responders.Count == 0) throw new InvalidOperationException("FakeChatClient: no responses queued");
        Func<ChatResponse> responder = _responders.Dequeue();
        try
        {
            return Task.FromResult(responder());
        }
        catch (Exception ex)
        {
            return Task.FromException<ChatResponse>(ex);
        }
    }

#pragma warning disable CS1998 // async method without await — yield break is the body
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield break;
    }
#pragma warning restore CS1998

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
