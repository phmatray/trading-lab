using TradingStrat.Web.Models.State;

namespace TradingStrat.Web.Services.State;

public class ChatStateService
{
    private const string STORAGE_KEY = "tradingstrat_chat_history";
    private const int MAX_MESSAGES = 50;
    private readonly LocalStorageService _localStorage;
    private ChatHistory? _cachedHistory;

    public event Action? OnHistoryChanged;

    public ChatStateService(LocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<ChatHistory> GetHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cachedHistory != null)
        {
            return _cachedHistory;
        }

        var stored = await _localStorage.GetItemAsync<ChatHistory>(
            STORAGE_KEY,
            cancellationToken);

        _cachedHistory = stored ?? new ChatHistory();
        return _cachedHistory;
    }

    public async Task AddMessageAsync(
        string content,
        bool isUser,
        CancellationToken cancellationToken = default)
    {
        var history = await GetHistoryAsync(cancellationToken);

        history.Messages.Add(new ChatMessage
        {
            Content = content,
            IsUser = isUser,
            Timestamp = DateTime.UtcNow
        });

        // Enforce message limit
        if (history.Messages.Count > MAX_MESSAGES)
        {
            history.Messages.RemoveRange(0, history.Messages.Count - MAX_MESSAGES);
        }

        history.LastUpdated = DateTime.UtcNow;

        await _localStorage.SetItemAsync(STORAGE_KEY, history, cancellationToken);
        _cachedHistory = history;

        NotifyHistoryChanged();
    }

    public async Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        _cachedHistory = new ChatHistory();
        await _localStorage.SetItemAsync(STORAGE_KEY, _cachedHistory, cancellationToken);
        NotifyHistoryChanged();
    }

    private void NotifyHistoryChanged()
    {
        OnHistoryChanged?.Invoke();
    }
}
