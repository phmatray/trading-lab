namespace TradingStrat.Domain.Entities;

/// <summary>
/// Domain entity representing a single message in a conversation with the AI trading assistant.
/// Used for conversation history persistence and context building.
/// Maps to the ChatMessages table in the SQLite database.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Database primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Session identifier to group related messages in a conversation.
    /// Required field.
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// Role of the message sender: "user" or "assistant".
    /// Required field.
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// The message content (text).
    /// Required field.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Optional ticker symbol if the message is related to a specific security.
    /// Used for context-aware responses.
    /// </summary>
    public string? Ticker { get; set; }

    /// <summary>
    /// Timestamp when this message was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
