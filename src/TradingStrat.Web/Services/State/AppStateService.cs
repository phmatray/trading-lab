using TradingStrat.Web.Models.State;

namespace TradingStrat.Web.Services.State;

/// <summary>
/// Service for managing application-wide state (current ticker, strategy selection).
/// </summary>
public class AppStateService : StateServiceBase<AppState>
{
    private const string STORAGE_KEY = "tradingstrat_app_state";

    public AppStateService(LocalStorageService localStorage)
        : base(localStorage, STORAGE_KEY)
    {
    }

    /// <summary>
    /// Sets the current ticker and persists the state.
    /// </summary>
    public async Task SetCurrentTickerAsync(
        string ticker,
        CancellationToken cancellationToken = default)
    {
        AppState state = await GetStateAsync(cancellationToken);
        state.CurrentTicker = ticker;
        await SaveStateAsync(state, cancellationToken);
    }

    /// <summary>
    /// Sets the current strategy and parameters, then persists the state.
    /// </summary>
    public async Task SetCurrentStrategyAsync(
        string strategyType,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        AppState state = await GetStateAsync(cancellationToken);
        state.CurrentStrategyType = strategyType;
        state.CurrentStrategyParameters = parameters;
        await SaveStateAsync(state, cancellationToken);
    }

    /// <summary>
    /// Sets the backtest context from the last backtest execution.
    /// Enables quick actions like "Create Portfolio from Backtest".
    /// </summary>
    public async Task SetBacktestContextAsync(
        BacktestContext context,
        CancellationToken cancellationToken = default)
    {
        AppState state = await GetStateAsync(cancellationToken);
        state.LastBacktestContext = context;
        await AddRecentTickerAsync(context.Ticker, cancellationToken);
        await SaveStateAsync(state, cancellationToken);
    }

    /// <summary>
    /// Sets the optimization context from the last optimization execution.
    /// Enables quick actions like "Apply Best Parameters".
    /// </summary>
    public async Task SetOptimizationContextAsync(
        OptimizationContext context,
        CancellationToken cancellationToken = default)
    {
        AppState state = await GetStateAsync(cancellationToken);
        state.LastOptimizationContext = context;
        await SaveStateAsync(state, cancellationToken);
    }

    /// <summary>
    /// Adds a ticker to the recent tickers list (max 10).
    /// Most recent tickers appear first.
    /// </summary>
    public async Task AddRecentTickerAsync(
        string ticker,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return;
        }

        AppState state = await GetStateAsync(cancellationToken);

        // Remove if already exists (to move to front)
        state.RecentTickers.Remove(ticker);

        // Add to front
        state.RecentTickers.Insert(0, ticker);

        // Keep only last 10
        if (state.RecentTickers.Count > 10)
        {
            state.RecentTickers = state.RecentTickers.Take(10).ToList();
        }

        await SaveStateAsync(state, cancellationToken);
    }

    /// <summary>
    /// Gets the list of recent tickers (max 10).
    /// </summary>
    public async Task<List<string>> GetRecentTickersAsync(
        CancellationToken cancellationToken = default)
    {
        AppState state = await GetStateAsync(cancellationToken);
        return state.RecentTickers;
    }
}
