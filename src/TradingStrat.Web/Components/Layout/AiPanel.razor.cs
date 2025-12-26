using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Layout;

public partial class AiPanel : ComponentBase, IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private ChatStateService ChatState { get; set; } = null!;

    [Parameter] public string? CurrentTicker { get; set; }
    [Parameter] public string? CurrentContext { get; set; }
    [Parameter] public Dictionary<string, object>? ContextData { get; set; }
    [Parameter] public bool IsCollapsed { get; set; }
    [Parameter] public EventCallback<bool> IsCollapsedChanged { get; set; }

    // AI Recommendation parameters
    [Parameter] public string CurrentRegime { get; set; } = "NEUTRAL";
    [Parameter] public string? CurrentRecommendation { get; set; }
    [Parameter] public int? Confidence { get; set; }
    [Parameter] public List<string>? Reasons { get; set; }

    // SignalR chat (migrated from AiAssistantWidget)
    private HubConnection? _hubConnection;
    private ElementReference _messagesContainer;
    private bool _isStreaming = false;
    private string _userInput = string.Empty;
    private string _streamingMessage = string.Empty;
    private string _sessionId = Guid.NewGuid().ToString();
    private List<ChatDisplayMessage> _messages = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                // Load chat history from localStorage
                var history = await ChatState.GetHistoryAsync();
                _messages = history.Messages.Select(m => new ChatDisplayMessage
                {
                    Content = m.Content,
                    IsUser = m.IsUser
                }).ToList();
                _sessionId = history.SessionId;

                StateHasChanged();

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(Navigation.ToAbsoluteUri("/chathub"))
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<string>("ReceiveMessageToken", (token) =>
                {
                    _streamingMessage += token;
                    InvokeAsync(StateHasChanged);
                });

                _hubConnection.On("MessageComplete", async () =>
                {
                    await InvokeAsync(async () =>
                    {
                        _messages.Add(new ChatDisplayMessage
                        {
                            Content = _streamingMessage,
                            IsUser = false
                        });

                        await ChatState.AddMessageAsync(_streamingMessage, isUser: false);

                        _streamingMessage = string.Empty;
                        _isStreaming = false;
                        StateHasChanged();
                        _ = ScrollToBottomAsync();
                    });
                });

                await _hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to chat hub: {ex.Message}");
            }
        }
    }

    private async Task ToggleCollapse()
    {
        IsCollapsed = !IsCollapsed;
        await IsCollapsedChanged.InvokeAsync(IsCollapsed);
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(_userInput) || _hubConnection is null)
        {
            return;
        }

        _messages.Add(new ChatDisplayMessage { Content = _userInput, IsUser = true });
        string messageToSend = _userInput;
        await ChatState.AddMessageAsync(messageToSend, isUser: true);

        _userInput = string.Empty;
        _isStreaming = true;
        _streamingMessage = string.Empty;

        await _hubConnection.SendAsync("SendMessage", messageToSend, CurrentTicker, _sessionId);
        await ScrollToBottomAsync();
    }

    private async Task HandleKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendMessageAsync();
        }
    }

    private async Task ClearHistoryAsync()
    {
        _messages.Clear();
        await ChatState.ClearHistoryAsync();
        var newHistory = await ChatState.GetHistoryAsync();
        _sessionId = newHistory.SessionId;
    }

    private async Task ScrollToBottomAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("scrollToBottom", _messagesContainer);
        }
        catch (Exception ex)
        {
            // JS interop errors are often transient (e.g., element not yet rendered in DOM)
            // Log for debugging but don't notify user
            Console.WriteLine($"[JS INTEROP WARNING] ScrollToBottom failed: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }

    private string GetPanelClasses()
    {
        string baseClasses = "fixed top-16 right-0 bottom-0 z-30 transition-all duration-300";
        string widthClasses = IsCollapsed ? "w-12" : "w-96";
        return $"{baseClasses} {widthClasses}";
    }

    private string GetRegimeColorClass() => CurrentRegime.ToUpperInvariant() switch
    {
        "BULLISH" => "text-green-600 dark:text-green-500",
        "BEARISH" => "text-red-600 dark:text-red-500",
        _ => "text-gray-600 dark:text-gray-400"
    };

    private string GetRegimeIcon() => CurrentRegime.ToUpperInvariant() switch
    {
        "BULLISH" => "🟢",
        "BEARISH" => "🔴",
        _ => "⚪"
    };

    private string GetRecommendationColorClass() => CurrentRecommendation?.ToUpperInvariant() switch
    {
        "BUY" => "text-green-600 dark:text-green-500",
        "SELL" or "REDUCE" or "EXIT" => "text-red-600 dark:text-red-500",
        _ => "text-gray-600 dark:text-gray-400"
    };

    private string GetConfidenceBarClass() => Confidence switch
    {
        >= 80 => "bg-green-500",
        >= 60 => "bg-yellow-500",
        _ => "bg-red-500"
    };

    private class ChatDisplayMessage
    {
        public string Content { get; set; } = string.Empty;
        public bool IsUser { get; set; }
    }
}
