using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Outbound port for chat history persistence.
/// Defines the contract for storing and retrieving conversation messages.
/// Implemented by infrastructure adapters (e.g., ChatHistoryRepository with EF Core).
/// </summary>
public interface IChatHistoryPort
{
    /// <summary>
    /// Saves a single message to the conversation history.
    /// </summary>
    /// <param name="message">The message to save (user or assistant).</param>
    Task SaveMessageAsync(ChatMessage message);

    /// <summary>
    /// Retrieves conversation history for a specific session.
    /// Messages are returned in chronological order (oldest first).
    /// </summary>
    /// <param name="sessionId">Session identifier to filter messages.</param>
    /// <param name="limit">Maximum number of recent messages to retrieve (default 20).</param>
    /// <returns>List of messages in chronological order.</returns>
    Task<List<ChatMessage>> GetConversationHistoryAsync(string sessionId, int limit = 20);

    /// <summary>
    /// Clears all conversation history for a specific session.
    /// Used when user requests to clear chat history.
    /// </summary>
    /// <param name="sessionId">Session identifier for which to clear history.</param>
    Task ClearHistoryAsync(string sessionId);
}
