using TradingStrat.Web.Models.State;

namespace TradingStrat.Web.Services.State;

/// <summary>
/// Service for managing AI chat history with localStorage persistence.
/// </summary>
public class ChatStateService : StateServiceBase<ChatHistory>
{
    private const string StorageKey = "tradingstrat_chat_history";
    private const int MaxMessages = 50;

    /// <summary>
    /// Event raised when chat history changes. Alias for OnStateChanged.
    /// </summary>
    public event Action? OnHistoryChanged
    {
        add => OnStateChanged += value;
        remove => OnStateChanged -= value;
    }

    public ChatStateService(LocalStorageService localStorage)
        : base(localStorage, StorageKey)
    {
    }

    /// <summary>
    /// Gets the chat history. Alias for GetStateAsync.
    /// </summary>
    public Task<ChatHistory> GetHistoryAsync(CancellationToken cancellationToken = default)
        => GetStateAsync(cancellationToken);

    /// <summary>
    /// Adds a message to chat history and enforces the maximum message limit.
    /// </summary>
    public async Task AddMessageAsync(
        string content,
        bool isUser,
        CancellationToken cancellationToken = default)
    {
        ChatHistory history = await GetStateAsync(cancellationToken);

        history.Messages.Add(new ChatMessage
        {
            Content = content,
            IsUser = isUser,
            Timestamp = DateTime.UtcNow
        });

        // Enforce message limit
        if (history.Messages.Count > MaxMessages)
        {
            history.Messages.RemoveRange(0, history.Messages.Count - MaxMessages);
        }

        history.LastUpdated = DateTime.UtcNow;
        await SaveStateAsync(history, cancellationToken);
    }

    /// <summary>
    /// Clears all chat history.
    /// </summary>
    public Task ClearHistoryAsync(CancellationToken cancellationToken = default)
        => ClearStateAsync(cancellationToken);
}
