namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for sending chat messages to the AI trading assistant with streaming responses.
/// Orchestrates conversation history retrieval, context building, LLM interaction, and persistence.
/// </summary>
public interface ISendChatMessageUseCase
{
    /// <summary>
    /// Sends a user message to the AI assistant and streams the response token-by-token.
    /// Handles conversation history, market context building, and message persistence.
    /// </summary>
    /// <param name="command">Command containing the user's message and optional context (ticker, session).</param>
    /// <param name="cancellationToken">Cancellation token for request cancellation.</param>
    /// <returns>Async enumerable of response tokens for real-time streaming to UI.</returns>
    IAsyncEnumerable<string> ExecuteStreamingAsync(
        SendChatMessageCommand command,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Command object for sending a chat message to the AI assistant.
/// </summary>
/// <param name="UserMessage">The user's message/question to send to the assistant.</param>
/// <param name="Ticker">Optional ticker symbol for context-aware responses about specific securities.</param>
/// <param name="SessionId">Optional session identifier to maintain conversation history (auto-generated if not provided).</param>
public record SendChatMessageCommand(
    string UserMessage,
    string? Ticker = null,
    string? SessionId = null);
