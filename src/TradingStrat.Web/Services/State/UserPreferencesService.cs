using Microsoft.Extensions.Options;
using TradingStrat.Application.Configuration;
using TradingStrat.Web.Models.State;

namespace TradingStrat.Web.Services.State;

/// <summary>
/// Service for managing user preferences with localStorage persistence.
/// Initializes preferences from application configuration if no saved preferences exist.
/// </summary>
public class UserPreferencesService : StateServiceBase<UserPreferences>
{
    private const string STORAGE_KEY = "tradingstrat_preferences";
    private readonly TradingConfiguration _configuration;

    /// <summary>
    /// Event raised when preferences change. Alias for OnStateChanged.
    /// </summary>
    public event Action? OnPreferencesChanged
    {
        add => OnStateChanged += value;
        remove => OnStateChanged -= value;
    }

    public UserPreferencesService(
        LocalStorageService localStorage,
        IOptions<TradingConfiguration> configuration)
        : base(localStorage, STORAGE_KEY)
    {
        _configuration = configuration.Value;
    }

    /// <summary>
    /// Gets user preferences. Alias for GetStateAsync.
    /// </summary>
    public Task<UserPreferences> GetPreferencesAsync(CancellationToken cancellationToken = default)
        => GetStateAsync(cancellationToken);

    /// <summary>
    /// Saves user preferences and updates the LastUpdated timestamp.
    /// </summary>
    public async Task SavePreferencesAsync(
        UserPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        preferences.LastUpdated = DateTime.UtcNow;
        await SaveStateAsync(preferences, cancellationToken);
    }

    /// <summary>
    /// Resets preferences to configuration defaults.
    /// </summary>
    public Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
        => ClearStateAsync(cancellationToken);

    /// <summary>
    /// Creates default preferences from application configuration.
    /// </summary>
    protected override Task<UserPreferences> CreateDefaultStateAsync()
    {
        UserPreferences defaults = new()
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

        return Task.FromResult(defaults);
    }
}
