using TradingStrat.Web.Models.State;

namespace TradingStrat.Web.Services.State;

/// <summary>
/// Service for managing selected portfolio state with localStorage persistence.
/// </summary>
public class PortfolioStateService : StateServiceBase<PortfolioState>
{
    private const string StorageKey = "tradingstrat_selected_portfolio";

    /// <summary>
    /// Event raised when the selected portfolio changes. Provides EventArgs for compatibility.
    /// </summary>
    public event EventHandler? OnPortfolioChanged;

    /// <summary>
    /// Gets the selected portfolio ID from memory.
    /// </summary>
    public int? SelectedPortfolioId { get; private set; }

    public PortfolioStateService(LocalStorageService localStorage)
        : base(localStorage, StorageKey)
    {
        // Subscribe to base class state change to update cached property
        OnStateChanged += () =>
        {
            OnPortfolioChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    /// <summary>
    /// Sets the selected portfolio ID and persists it to local storage.
    /// </summary>
    public async Task SetSelectedPortfolioAsync(
        int portfolioId,
        CancellationToken cancellationToken = default)
    {
        SelectedPortfolioId = portfolioId;
        await SaveStateAsync(new PortfolioState { SelectedPortfolioId = portfolioId }, cancellationToken);
    }

    /// <summary>
    /// Clears the selected portfolio from memory and local storage.
    /// </summary>
    public async Task ClearSelectedPortfolioAsync(CancellationToken cancellationToken = default)
    {
        SelectedPortfolioId = null;
        await ClearStateAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the selected portfolio ID from local storage.
    /// </summary>
    public async Task<int?> GetSelectedPortfolioIdAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedPortfolioId.HasValue)
        {
            return SelectedPortfolioId;
        }

        PortfolioState state = await GetStateAsync(cancellationToken);
        SelectedPortfolioId = state.SelectedPortfolioId;
        return SelectedPortfolioId;
    }
}
