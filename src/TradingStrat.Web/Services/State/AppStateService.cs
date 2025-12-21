using TradingStrat.Web.Models.State;

namespace TradingStrat.Web.Services.State;

public class AppStateService
{
    private const string STORAGE_KEY = "tradingstrat_app_state";
    private readonly LocalStorageService _localStorage;
    private AppState? _cachedState;

    public event Action? OnStateChanged;

    public AppStateService(LocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<AppState> GetStateAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cachedState != null)
        {
            return _cachedState;
        }

        var stored = await _localStorage.GetItemAsync<AppState>(
            STORAGE_KEY,
            cancellationToken);

        _cachedState = stored ?? new AppState();
        return _cachedState;
    }

    public async Task SetCurrentTickerAsync(
        string ticker,
        CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync(cancellationToken);
        state.CurrentTicker = ticker;
        await SaveStateAsync(state, cancellationToken);
    }

    public async Task SetCurrentStrategyAsync(
        string strategyType,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync(cancellationToken);
        state.CurrentStrategyType = strategyType;
        state.CurrentStrategyParameters = parameters;
        await SaveStateAsync(state, cancellationToken);
    }

    private async Task SaveStateAsync(
        AppState state,
        CancellationToken cancellationToken)
    {
        await _localStorage.SetItemAsync(STORAGE_KEY, state, cancellationToken);
        _cachedState = state;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
