using System.Runtime.CompilerServices;
using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using DomainChatMessage = TradingStrat.Domain.Entities.ChatMessage;
using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace TradingStrat.Infrastructure.Assistant;

/// <summary>
/// Infrastructure adapter for Anthropic's Claude API.
/// Implements IAssistantPort to provide LLM capabilities via the official Anthropic SDK
/// and Microsoft.Extensions.AI IChatClient abstraction.
/// Supports both streaming and non-streaming responses.
/// </summary>
public class AnthropicAdapter : IAssistantPort
{
    private readonly IChatClient _chatClient;
    private readonly AssistantConfiguration _config;

    public AnthropicAdapter(IOptions<AssistantConfiguration> config)
    {
        _config = config.Value;

        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            throw new InvalidOperationException(
                "Anthropic API key is not configured. " +
                "Please set Trading:Assistant:ApiKey in appsettings.json or via environment variables.");
        }

        // Create Anthropic client with API key
        var anthropicClient = new AnthropicClient { APIKey = _config.ApiKey };

        // Expose as IChatClient with specified model
        _chatClient = anthropicClient.AsIChatClient(_config.Model);
    }

    public async IAsyncEnumerable<string> StreamChatResponseAsync(
        string systemPrompt,
        List<DomainChatMessage> conversationHistory,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Build message list for IChatClient
        List<AIChatMessage> messages = BuildChatMessageList(systemPrompt, conversationHistory, userMessage);

        // Create chat options
        var options = new ChatOptions
        {
            MaxOutputTokens = _config.MaxTokens,
            Temperature = (float)_config.Temperature
        };

        // Stream the response
        await foreach (ChatResponseUpdate update in _chatClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            // Extract text content from streaming updates
            foreach (TextContent content in update.Contents.OfType<TextContent>())
            {
                if (!string.IsNullOrEmpty(content.Text))
                {
                    yield return content.Text;
                }
            }
        }
    }

    public async Task<string> GetChatResponseAsync(
        string systemPrompt,
        List<DomainChatMessage> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build message list for IChatClient
            List<AIChatMessage> messages = BuildChatMessageList(systemPrompt, conversationHistory, userMessage);

            // Create chat options
            var options = new ChatOptions
            {
                MaxOutputTokens = _config.MaxTokens,
                Temperature = (float)_config.Temperature
            };

            // Get non-streaming response
            ChatResponse response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);

            // Extract text from response - ChatResponse has a ToString() that returns the text
            return response.ToString();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to get response from Anthropic API: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Builds a list of Microsoft.Extensions.AI ChatMessage objects from the conversation history.
    /// Includes the system prompt as the first message.
    /// </summary>
    private static List<AIChatMessage> BuildChatMessageList(
        string systemPrompt,
        List<DomainChatMessage> conversationHistory,
        string userMessage)
    {
        var messages = new List<AIChatMessage>();

        // Add system prompt as first message
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new AIChatMessage(ChatRole.System, systemPrompt));
        }

        // Convert conversation history
        foreach (DomainChatMessage msg in conversationHistory)
        {
            ChatRole role = msg.Role.ToLowerInvariant() switch
            {
                "user" => ChatRole.User,
                "assistant" => ChatRole.Assistant,
                "system" => ChatRole.System,
                _ => ChatRole.User
            };

            messages.Add(new AIChatMessage(role, msg.Content));
        }

        // Add current user message
        messages.Add(new AIChatMessage(ChatRole.User, userMessage));

        return messages;
    }
}
