using Microsoft.Extensions.Options;
using TradingStrat.Application.Configuration;
using TradingStrat.Web.Models.State;

namespace TradingStrat.Web.Services.State;

public class UserPreferencesService
{
    private const string STORAGE_KEY = "tradingstrat_preferences";
    private readonly LocalStorageService _localStorage;
    private readonly TradingConfiguration _configuration;
    private UserPreferences? _cachedPreferences;

    public event Action? OnPreferencesChanged;

    public UserPreferencesService(
        LocalStorageService localStorage,
        IOptions<TradingConfiguration> configuration)
    {
        _localStorage = localStorage;
        _configuration = configuration.Value;
    }

    public async Task<UserPreferences> GetPreferencesAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cachedPreferences != null)
        {
            return _cachedPreferences;
        }

        UserPreferences? stored = await _localStorage.GetItemAsync<UserPreferences>(
            STORAGE_KEY,
            cancellationToken);

        if (stored != null)
        {
            _cachedPreferences = stored;
            return stored;
        }

        // Initialize from configuration defaults
        _cachedPreferences = new UserPreferences
        {
            DefaultTicker = _configuration.DefaultTicker,
            DefaultIsin = _configuration.DefaultIsin,
            BacktestDefaults = new BacktestDefaults
            {
                InitialCapital = _configuration.Backtest.InitialCapital,
                CommissionPercentage = _configuration.Backtest.CommissionPercentage,
                MinimumCommission = _configuration.Backtest.MinimumCommission
            }
        };

        // Save initialized preferences
        await SavePreferencesAsync(_cachedPreferences, cancellationToken);

        return _cachedPreferences;
    }

    public async Task SavePreferencesAsync(
        UserPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        preferences.LastUpdated = DateTime.UtcNow;
        await _localStorage.SetItemAsync(STORAGE_KEY, preferences, cancellationToken);
        _cachedPreferences = preferences;
        NotifyPreferencesChanged();
    }

    public async Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        _cachedPreferences = null;
        await _localStorage.RemoveItemAsync(STORAGE_KEY, cancellationToken);
        NotifyPreferencesChanged();
    }

    private void NotifyPreferencesChanged()
    {
        OnPreferencesChanged?.Invoke();
    }
}
