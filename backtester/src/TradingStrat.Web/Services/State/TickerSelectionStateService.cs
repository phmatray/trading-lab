using TradingStrat.Web.Models.State;

namespace TradingStrat.Web.Services.State;

/// <summary>
/// Service for managing selected ticker state for AI analysis with localStorage persistence.
/// </summary>
public class TickerSelectionStateService : StateServiceBase<TickerSelectionState>
{
    private const string StorageKey = "tradingstrat_selected_ticker";

    /// <summary>
    /// Event raised when the selected ticker changes. Provides EventArgs for compatibility.
    /// </summary>
    public event EventHandler? OnTickerChanged;

    /// <summary>
    /// Gets the selected ticker symbol from memory.
    /// </summary>
    public string? SelectedTicker { get; private set; }

    public TickerSelectionStateService(LocalStorageService localStorage)
        : base(localStorage, StorageKey)
    {
        // Subscribe to base class state change to update cached property
        OnStateChanged += () =>
        {
            OnTickerChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    /// <summary>
    /// Sets the selected ticker symbol and persists it to local storage.
    /// </summary>
    public async Task SetSelectedTickerAsync(
        string ticker,
        CancellationToken cancellationToken = default)
    {
        SelectedTicker = ticker;
        await SaveStateAsync(new TickerSelectionState { SelectedTicker = ticker }, cancellationToken);
    }

    /// <summary>
    /// Clears the selected ticker from memory and local storage.
    /// </summary>
    public async Task ClearSelectedTickerAsync(CancellationToken cancellationToken = default)
    {
        SelectedTicker = null;
        await ClearStateAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the selected ticker symbol from local storage.
    /// </summary>
    public async Task<string?> GetSelectedTickerAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(SelectedTicker))
        {
            return SelectedTicker;
        }

        TickerSelectionState state = await GetStateAsync(cancellationToken);
        SelectedTicker = state.SelectedTicker;
        return SelectedTicker;
    }
}
