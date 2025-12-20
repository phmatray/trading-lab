namespace TradingStrat.Application.Services;

/// <summary>
/// Strongly-typed configuration for the AI trading assistant.
/// Binds to the "Trading:Assistant" section in appsettings.json.
/// </summary>
public class AssistantConfiguration
{
    /// <summary>
    /// Anthropic API key for Claude access.
    /// Should be set via environment variable or user secrets in production.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Claude model identifier (e.g., "claude-sonnet-4-5-20250929").
    /// Determines model capabilities, cost, and response quality.
    /// </summary>
    public string Model { get; set; } = "claude-sonnet-4-5-20250929";

    /// <summary>
    /// Maximum number of tokens in the assistant's response.
    /// Higher values allow longer responses but increase cost.
    /// Recommended: 2048 for balanced responses.
    /// </summary>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// Temperature for response generation (0.0-1.0).
    /// Lower values (0.3-0.5) = more focused, deterministic responses.
    /// Higher values (0.7-1.0) = more creative, varied responses.
    /// Recommended: 0.7 for conversational balance.
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum number of previous messages to include in conversation history.
    /// Limits context window size to control cost and latency.
    /// Recommended: 20 for conversational continuity without excessive tokens.
    /// </summary>
    public int ConversationHistoryLimit { get; set; } = 20;
}
