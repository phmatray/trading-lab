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
}
