using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Models.State;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Layout;

// Note: PanelMode enum is defined in AiPanel.razor.cs (shared with StrategyCopiloPanel)
public partial class StrategyCopiloPanel : ComponentBase, IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private ChatStateService ChatState { get; set; } = null!;
    [Inject] private TickerSelectionStateService TickerSelectionState { get; set; } = null!;
    [Inject] private IAnalyzeTickerUseCase AnalyzeTickerUseCase { get; set; } = null!;

    [Parameter] public string? CurrentTicker { get; set; }
    [Parameter] public string? CurrentContext { get; set; }
    [Parameter] public string CurrentRegime { get; set; } = "NEUTRAL";
    [Parameter] public string? CurrentRecommendation { get; set; }
    [Parameter] public int? Confidence { get; set; }
    [Parameter] public List<string>? Reasons { get; set; }

    // Panel mode state
    private PanelMode _panelMode = PanelMode.Chat;

    // Analysis mode state
    private string _selectedTicker = string.Empty;
    private TickerAnalysis? _tickerAnalysis;
    private bool _isAnalyzing = false;
    private string? _analysisError;

    // SignalR chat
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
                ChatHistory history = await ChatState.GetHistoryAsync();
                _messages = history.Messages.Select(m => new ChatDisplayMessage
                {
                    Content = m.Content,
                    IsUser = m.IsUser
                }).ToList();
                _sessionId = history.SessionId;

                // Load selected ticker if any
                _selectedTicker = await TickerSelectionState.GetSelectedTickerAsync() ?? string.Empty;

                StateHasChanged();

                // Only initialize chat hub if starting in Chat mode (lazy initialization)
                if (_panelMode == PanelMode.Chat)
                {
                    await InitializeChatHubAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize Strategy Copilot Panel: {ex.Message}");
            }
        }
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
        ChatHistory newHistory = await ChatState.GetHistoryAsync();
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
            Console.WriteLine($"[JS INTEROP WARNING] ScrollToBottom failed: {ex.Message}");
        }
    }

    private async Task OnModeChanged(PanelMode newMode)
    {
        _panelMode = newMode;

        // Initialize chat hub only when switching to Chat mode (lazy initialization)
        if (_panelMode == PanelMode.Chat && _hubConnection is null)
        {
            await InitializeChatHubAsync();
        }

        StateHasChanged();
    }

    private async Task AnalyzeTickerAsync()
    {
        if (string.IsNullOrWhiteSpace(_selectedTicker))
        {
            _analysisError = "Please enter a ticker symbol";
            return;
        }

        _isAnalyzing = true;
        _analysisError = null;
        _tickerAnalysis = null;

        try
        {
            Result<TickerAnalysis> result = await AnalyzeTickerUseCase.ExecuteAsync(_selectedTicker);

            if (result.IsSuccess)
            {
                _tickerAnalysis = result.Value;
                await TickerSelectionState.SetSelectedTickerAsync(_selectedTicker);
            }
            else
            {
                _analysisError = string.Join(", ", result.Errors.Select(e => e.Message));
            }
        }
        catch (Exception ex)
        {
            _analysisError = $"Analysis failed: {ex.Message}";
        }
        finally
        {
            _isAnalyzing = false;
            StateHasChanged();
        }
    }

    private async Task InitializeChatHubAsync()
    {
        try
        {
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

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }

    private string GetRecommendationColorClass()
    {
        if (_tickerAnalysis is null)
        {
            return "text-gray-600 dark:text-gray-400";
        }

        return _tickerAnalysis.Recommendation.ToUpperInvariant() switch
        {
            "BUY" => "text-green-600 dark:text-green-500",
            "SELL" or "REDUCE" or "EXIT" => "text-red-600 dark:text-red-500",
            _ => "text-gray-600 dark:text-gray-400"
        };
    }

    private string GetConfidenceBarClass()
    {
        if (_tickerAnalysis is null)
        {
            return "bg-gray-500";
        }

        return _tickerAnalysis.Confidence switch
        {
            >= 80 => "bg-green-500",
            >= 60 => "bg-yellow-500",
            _ => "bg-red-500"
        };
    }

    private class ChatDisplayMessage
    {
        public string Content { get; set; } = string.Empty;
        public bool IsUser { get; set; }
    }
}
