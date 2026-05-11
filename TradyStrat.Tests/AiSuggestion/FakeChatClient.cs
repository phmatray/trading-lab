using Microsoft.Extensions.AI;

namespace TradyStrat.Tests.AiSuggestion;

/// <summary>
/// In-process IChatClient that simulates the model invoking the tool(s) supplied
/// via ChatOptions.Tools. The invoker delegate receives the AIFunction list so the
/// test can call whichever tool it wishes.
/// </summary>
public sealed class FakeChatClient(Func<IList<AIFunction>, Task> invoker) : IChatClient
{
    public ChatOptions? LastOptions { get; private set; }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        LastOptions = options;

        var tools = options?.Tools
            ?.OfType<AIFunction>()
            .ToList()
            ?? (IList<AIFunction>)[];

        await invoker(tools);

        return new ChatResponse();
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Streaming not supported in FakeChatClient.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
