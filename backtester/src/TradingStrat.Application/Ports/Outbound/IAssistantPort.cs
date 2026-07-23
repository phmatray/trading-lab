using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Outbound port for AI assistant integration (LLM provider abstraction).
/// Defines the contract for communicating with language models like Claude or GPT.
/// Implemented by infrastructure adapters (e.g., AnthropicAdapter).
/// Supports both streaming and non-streaming responses.
/// </summary>
public interface IAssistantPort
{
    /// <summary>
    /// Streams chat response token-by-token from the LLM.
    /// Used for conversational AI interface with real-time updates.
    /// </summary>
    /// <param name="systemPrompt">System-level instructions defining assistant behavior and capabilities.</param>
    /// <param name="conversationHistory">Previous messages in the conversation for context.</param>
    /// <param name="userMessage">Current user message to respond to.</param>
    /// <param name="cancellationToken">Cancellation token for request cancellation.</param>
    /// <returns>Async enumerable of text tokens (words or phrases) as they're generated.</returns>
    IAsyncEnumerable<string> StreamChatResponseAsync(
        string systemPrompt,
        List<ChatMessage> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets complete chat response from the LLM (non-streaming).
    /// Used for structured outputs like strategy analysis where full response is needed before parsing.
    /// </summary>
    /// <param name="systemPrompt">System-level instructions defining assistant behavior and capabilities.</param>
    /// <param name="conversationHistory">Previous messages in the conversation for context.</param>
    /// <param name="userMessage">Current user message to respond to.</param>
    /// <param name="cancellationToken">Cancellation token for request cancellation.</param>
    /// <returns>Complete response text from the LLM.</returns>
    Task<string> GetChatResponseAsync(
        string systemPrompt,
        List<ChatMessage> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default);
}
