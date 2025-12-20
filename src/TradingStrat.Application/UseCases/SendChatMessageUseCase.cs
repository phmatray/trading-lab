using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for sending chat messages to the AI trading assistant with streaming responses.
/// Orchestrates: conversation history retrieval, market context building, LLM streaming, and message persistence.
/// </summary>
public class SendChatMessageUseCase : ISendChatMessageUseCase
{
    private readonly IAssistantPort _assistantPort;
    private readonly IChatHistoryPort _chatHistoryPort;
    private readonly PortfolioContextBuilder _contextBuilder;
    private readonly AssistantConfiguration _config;

    public SendChatMessageUseCase(
        IAssistantPort assistantPort,
        IChatHistoryPort chatHistoryPort,
        PortfolioContextBuilder contextBuilder,
        IOptions<AssistantConfiguration> config)
    {
        _assistantPort = assistantPort;
        _chatHistoryPort = chatHistoryPort;
        _contextBuilder = contextBuilder;
        _config = config.Value;
    }

    public async IAsyncEnumerable<string> ExecuteStreamingAsync(
        SendChatMessageCommand command,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Generate or use existing session ID
        string sessionId = command.SessionId ?? Guid.NewGuid().ToString();

        // Save user message to history
        ChatMessage userMessage = new ChatMessage
        {
            SessionId = sessionId,
            Role = "user",
            Content = command.UserMessage,
            Ticker = command.Ticker,
            Timestamp = DateTime.UtcNow
        };
        await _chatHistoryPort.SaveMessageAsync(userMessage);

        // Build system prompt with market context if ticker provided
        string systemPrompt = PromptTemplates.ChatAssistantSystemPrompt;
        if (!string.IsNullOrEmpty(command.Ticker))
        {
            try
            {
                string marketContext = await _contextBuilder.BuildContextForTicker(command.Ticker);
                systemPrompt += $"\n\nCURRENT MARKET CONTEXT:\n{marketContext}";
            }
            catch (Exception ex)
            {
                // If context building fails, continue without market context
                systemPrompt += $"\n\nNote: Unable to load market data for {command.Ticker}: {ex.Message}";
            }
        }

        // Retrieve conversation history
        List<ChatMessage> history = await _chatHistoryPort.GetConversationHistoryAsync(
            sessionId,
            _config.ConversationHistoryLimit);

        // Stream response from AI assistant
        StringBuilder assistantResponse = new StringBuilder();
        await foreach (string token in _assistantPort.StreamChatResponseAsync(
            systemPrompt,
            history,
            command.UserMessage,
            cancellationToken))
        {
            assistantResponse.Append(token);
            yield return token;
        }

        // Save assistant response to history
        ChatMessage assistantMessage = new ChatMessage
        {
            SessionId = sessionId,
            Role = "assistant",
            Content = assistantResponse.ToString(),
            Ticker = command.Ticker,
            Timestamp = DateTime.UtcNow
        };
        await _chatHistoryPort.SaveMessageAsync(assistantMessage);
    }
}
