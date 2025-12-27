using Microsoft.AspNetCore.SignalR;
using TradingStrat.Application.Ports.Inbound;

namespace TradingStrat.Web.Hubs;

/// <summary>
/// SignalR hub for real-time AI chat streaming.
/// Orchestrates message sending and token-by-token response streaming.
/// </summary>
public class ChatHub : Hub
{
    private readonly ISendChatMessageUseCase _sendChatMessageUseCase;

    public ChatHub(ISendChatMessageUseCase sendChatMessageUseCase)
    {
        _sendChatMessageUseCase = sendChatMessageUseCase;
    }

    /// <summary>
    /// Sends a chat message and streams the AI response token-by-token to the caller.
    /// </summary>
    /// <param name="userMessage">The user's message text.</param>
    /// <param name="ticker">Optional ticker symbol for market context.</param>
    /// <param name="sessionId">Optional session ID for conversation history.</param>
    public async Task SendMessage(string userMessage, string? ticker = null, string? sessionId = null)
    {
        try
        {
            SendChatMessageCommand command = new SendChatMessageCommand(userMessage, ticker, sessionId);

            await foreach (string token in _sendChatMessageUseCase.ExecuteStreamingAsync(command))
            {
                await Clients.Caller.SendAsync("ReceiveMessageToken", token);
            }

            await Clients.Caller.SendAsync("MessageComplete");
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("ReceiveError", ex.Message);
        }
    }
}
