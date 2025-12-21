namespace TradingStrat.Web.Services.State;

/// <summary>
/// Service for managing portfolio state in the application.
/// </summary>
public class PortfolioStateService
{
    private const string STORAGE_KEY = "tradingstrat_selected_portfolio";
    private readonly LocalStorageService _localStorage;

    /// <summary>
    /// Event raised when the selected portfolio changes.
    /// </summary>
    public event EventHandler? OnPortfolioChanged;

    /// <summary>
    /// Gets or sets the selected portfolio ID.
    /// </summary>
    public int? SelectedPortfolioId { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PortfolioStateService"/> class.
    /// </summary>
    /// <param name="localStorage">The local storage service.</param>
    public PortfolioStateService(LocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>
    /// Sets the selected portfolio ID and persists it to local storage.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID to select.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SetSelectedPortfolioAsync(
        int portfolioId,
        CancellationToken cancellationToken = default)
    {
        SelectedPortfolioId = portfolioId;
        await _localStorage.SetItemAsync(STORAGE_KEY, portfolioId, cancellationToken);
        OnPortfolioChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clears the selected portfolio from memory and local storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ClearSelectedPortfolioAsync(
        CancellationToken cancellationToken = default)
    {
        SelectedPortfolioId = null;
        await _localStorage.RemoveItemAsync(STORAGE_KEY, cancellationToken);
        OnPortfolioChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the selected portfolio ID from local storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected portfolio ID, or null if none is selected.</returns>
    public async Task<int?> GetSelectedPortfolioIdAsync(
        CancellationToken cancellationToken = default)
    {
        if (SelectedPortfolioId.HasValue)
        {
            return SelectedPortfolioId;
        }

        int? portfolioId = await _localStorage.GetItemAsync<int?>(
            STORAGE_KEY,
            cancellationToken);

        SelectedPortfolioId = portfolioId;
        return portfolioId;
    }
}
