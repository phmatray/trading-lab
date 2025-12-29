using TradingStrat.Web.Models.State;

namespace TradingStrat.Web.Services.State;

/// <summary>
/// Service for managing right panel state with localStorage persistence.
/// Follows StateServiceBase pattern for consistency.
/// </summary>
public class RightPanelStateService : StateServiceBase<RightPanelState>
{
    private const string StorageKey = "tradingstrat_rightpanel_state";
    private const string LegacyAiPanelKey = "layout_aipanel_collapsed";
    private readonly LocalStorageService _localStorage;

    public RightPanelStateService(LocalStorageService localStorage)
        : base(localStorage, StorageKey)
    {
        _localStorage = localStorage;
    }

    /// <summary>
    /// Sets the active tab and saves state.
    /// </summary>
    public async Task SetActiveTabAsync(
        RightPanelTab tab,
        CancellationToken cancellationToken = default)
    {
        RightPanelState state = await GetStateAsync(cancellationToken);
        state.ActiveTab = tab;
        state.LastUpdated = DateTime.UtcNow;
        await SaveStateAsync(state, cancellationToken);
    }

    /// <summary>
    /// Sets the collapse state and saves.
    /// </summary>
    public async Task SetCollapsedAsync(
        bool isCollapsed,
        CancellationToken cancellationToken = default)
    {
        RightPanelState state = await GetStateAsync(cancellationToken);
        state.IsCollapsed = isCollapsed;
        state.LastUpdated = DateTime.UtcNow;
        await SaveStateAsync(state, cancellationToken);
    }

    /// <summary>
    /// Toggles the collapse state and saves.
    /// </summary>
    public async Task ToggleCollapsedAsync(CancellationToken cancellationToken = default)
    {
        RightPanelState state = await GetStateAsync(cancellationToken);
        state.IsCollapsed = !state.IsCollapsed;
        state.LastUpdated = DateTime.UtcNow;
        await SaveStateAsync(state, cancellationToken);
    }

    /// <summary>
    /// Gets the current active tab without loading entire state.
    /// </summary>
    public async Task<RightPanelTab> GetActiveTabAsync(
        CancellationToken cancellationToken = default)
    {
        RightPanelState state = await GetStateAsync(cancellationToken);
        return state.ActiveTab;
    }

    /// <summary>
    /// Gets the current collapse state without loading entire state.
    /// </summary>
    public async Task<bool> IsCollapsedAsync(
        CancellationToken cancellationToken = default)
    {
        RightPanelState state = await GetStateAsync(cancellationToken);
        return state.IsCollapsed;
    }

    /// <summary>
    /// Creates default state with migration from legacy AiPanel collapse state.
    /// </summary>
    protected override async Task<RightPanelState> CreateDefaultStateAsync()
    {
        // Try to migrate old AiPanel collapse state
        bool? oldCollapsed = await SafeLocalStorageCall(
            async () => await _localStorage.GetItemAsync<bool?>(LegacyAiPanelKey),
            fallbackValue: null);

        return new RightPanelState
        {
            IsCollapsed = oldCollapsed ?? false,
            ActiveTab = RightPanelTab.StrategyCopilot,
            LastUpdated = DateTime.UtcNow
        };
    }
}
