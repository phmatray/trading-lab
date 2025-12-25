using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Configuration;
using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Pages;

public partial class Settings : ComponentBase
{
    [Inject] private UserPreferencesService PreferencesService { get; set; } = null!;
    [Inject] private LocalStorageService LocalStorage { get; set; } = null!;
    [Inject] private IOptions<TradingConfiguration> Configuration { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private Models.State.UserPreferences _preferences = new();
    private string? _successMessage;
    private string? _errorMessage;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _preferences = await PreferencesService.GetPreferencesAsync();
            StateHasChanged();
        }
    }

    private async Task HandleSaveSettings()
    {
        Console.WriteLine("🔵 HandleSaveSettings called!");
        _errorMessage = null;
        _successMessage = null;

        try
        {
            Console.WriteLine($"🔵 Saving preferences: Ticker={_preferences.DefaultTicker}");
            _preferences.LastUpdated = DateTime.UtcNow;
            await PreferencesService.SavePreferencesAsync(_preferences);
            _successMessage = "Settings saved successfully!";
            Console.WriteLine("🔵 Settings saved successfully!");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔴 Error saving settings: {ex.Message}");
            _errorMessage = $"Error saving settings: {ex.Message}";
            StateHasChanged();
        }
    }

    private async Task HandleResetToDefaults()
    {
        _errorMessage = null;
        _successMessage = null;

        try
        {
            TradingConfiguration config = Configuration.Value;
            _preferences = new Models.State.UserPreferences
            {
                DefaultTicker = config.DefaultTicker,
                DefaultIsin = config.DefaultIsin,
                BacktestDefaults = new Models.State.BacktestDefaults
                {
                    InitialCapital = config.Backtest.InitialCapital,
                    CommissionPercentage = config.Backtest.CommissionPercentage,
                    MinimumCommission = config.Backtest.MinimumCommission,
                    PreferredStrategy = "ma"
                },
                ChartPreferences = new Models.State.ChartPreferences
                {
                    ShowVolume = true,
                    ShowIndicators = true,
                    DefaultInterval = "D"
                },
                Theme = "system"
            };

            await PreferencesService.SavePreferencesAsync(_preferences);
            _successMessage = "Settings reset to defaults successfully!";
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error resetting settings: {ex.Message}";
        }
    }

    private async Task HandleClearAllData()
    {
        _errorMessage = null;
        _successMessage = null;

        try
        {
            // Clear all localStorage keys
            await LocalStorage.RemoveItemAsync("tradingstrat_preferences");
            await LocalStorage.RemoveItemAsync("tradingstrat_app_state");
            await LocalStorage.RemoveItemAsync("tradingstrat_chat_history");
            await LocalStorage.RemoveItemAsync("tradingstrat_form_states");

            // Clear cache
            LocalStorage.ClearCache();

            _successMessage = "All data cleared successfully! Reloading page...";

            // Reload page to reset state
            await Task.Delay(1500);
            Navigation.NavigateTo("/", forceLoad: true);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error clearing data: {ex.Message}";
        }
    }
}
