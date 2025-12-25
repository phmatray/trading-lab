using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Shared;

public partial class AiAssistantWidget : ComponentBase, IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private ChatStateService ChatState { get; set; } = null!;

    [Parameter]
    public string? CurrentTicker { get; set; }

    private HubConnection? _hubConnection;
    private ElementReference _messagesContainer;
    private bool _isMinimized = true;
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
                Models.State.ChatHistory history = await ChatState.GetHistoryAsync();
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

                        // Save assistant message to localStorage
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
                // Connection will be retried automatically due to WithAutomaticReconnect()
            }
        }
    }

    private void ToggleMinimize()
    {
        _isMinimized = !_isMinimized;
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(_userInput) || _hubConnection is null)
        {
            return;
        }

        _messages.Add(new ChatDisplayMessage
        {
            Content = _userInput,
            IsUser = true
        });

        string messageToSend = _userInput;

        // Save user message to localStorage
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
        Models.State.ChatHistory newHistory = await ChatState.GetHistoryAsync();
        _sessionId = newHistory.SessionId;
    }

    private async Task ScrollToBottomAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("scrollToBottom", _messagesContainer);
        }
        catch
        {
            // JS interop may fail if component is being disposed
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }

    private class ChatDisplayMessage
    {
        public string Content { get; set; } = string.Empty;
        public bool IsUser { get; set; }
    }
}
