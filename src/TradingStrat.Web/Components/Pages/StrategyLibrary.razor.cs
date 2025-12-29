using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Common;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;
using static TradingStrat.Web.Services.DebugLogger;

namespace TradingStrat.Web.Components.Pages;

public partial class StrategyLibrary
{
    [Inject]
    private ICustomStrategyManagementUseCase CustomStrategyUseCase { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    private enum StrategyTab
    {
        BuiltIn,
        Custom
    }

    private StrategyTab _activeTab = StrategyTab.BuiltIn;
    private bool _isLoading;
    private List<CustomStrategyResult> _customStrategies = [];

    // Delete dialog state
    private bool _showDeleteDialog;
    private int _strategyToDelete;
    private string _strategyToDeleteName = string.Empty;

    private readonly List<Shared.BreadcrumbNav.Breadcrumb> _breadcrumbs = new()
    {
        new() { Label = "Dashboard", Href = "/" },
        new() { Label = "Strategy Library", Href = "/strategies/library" }
    };

    private readonly List<BuiltInStrategyInfo> _builtInStrategies = new()
    {
        new BuiltInStrategyInfo
        {
            Name = "Moving Average Crossover",
            Key = "ma",
            Category = "Trend Following",
            Description = "Generates buy/sell signals when a fast moving average crosses above/below a slow moving average. Classic trend-following strategy.",
            DefaultParameters = new() { ["FastPeriod"] = "20", ["SlowPeriod"] = "50" }
        },
        new BuiltInStrategyInfo
        {
            Name = "RSI Strategy",
            Key = "rsi",
            Category = "Momentum",
            Description = "Buys when RSI is oversold (<30) and sells when overbought (>70). Identifies potential reversal points.",
            DefaultParameters = new() { ["Period"] = "14", ["OversoldThreshold"] = "30", ["OverboughtThreshold"] = "70" }
        },
        new BuiltInStrategyInfo
        {
            Name = "MACD Strategy",
            Key = "macd",
            Category = "Momentum",
            Description = "Generates signals based on MACD line crossing the signal line. Captures momentum shifts.",
            DefaultParameters = new() { ["FastPeriod"] = "12", ["SlowPeriod"] = "26", ["SignalPeriod"] = "9" }
        },
        new BuiltInStrategyInfo
        {
            Name = "Machine Learning (FastTree)",
            Key = "ml",
            Category = "AI/ML",
            Description = "Uses ML.NET FastTree gradient boosting with 26 technical indicators to predict next-day returns.",
            DefaultParameters = new() { ["BuyThreshold"] = "0.01", ["SellThreshold"] = "-0.01" }
        },
        new BuiltInStrategyInfo
        {
            Name = "Ichimoku Cloud",
            Key = "ichimoku",
            Category = "Trend Following",
            Description = "Japanese charting technique using Tenkan, Kijun, and Senkou spans to identify trend direction and support/resistance.",
            DefaultParameters = new() { ["TenkanPeriod"] = "9", ["KijunPeriod"] = "26", ["SenkouBPeriod"] = "52" }
        }
    };

    private class BuiltInStrategyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> DefaultParameters { get; set; } = new();
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadCustomStrategies();
    }

    private async Task LoadCustomStrategies()
    {
        _isLoading = true;

        try
        {
            Log("[StrategyLibrary] Loading custom strategies...");
            Result<List<CustomStrategyResult>> result = await CustomStrategyUseCase.GetAllStrategiesAsync();

            if (result.IsFailure)
            {
                Log($"[StrategyLibrary] ERROR loading strategies: {string.Join(", ", result.Errors.Select(e => e.Message))}");
                _customStrategies = new List<CustomStrategyResult>();
                _ = ShowErrorAsync(string.Join(", ", result.Errors.Select(e => e.Message)));
                return;
            }

            List<CustomStrategyResult> strategies = result.Value;
            Log($"[StrategyLibrary] Loaded {strategies.Count} strategies");

            _customStrategies = strategies.OrderByDescending(s => s.LastUpdatedAt).ToList();
            Log("[StrategyLibrary] Strategies sorted and assigned");
        }
        catch (Exception ex)
        {
            Log($"[StrategyLibrary] ERROR loading strategies: {ex.GetType().Name}: {ex.Message}");
            Log($"[StrategyLibrary] Stack: {ex.StackTrace}");

            // Initialize to empty list on error so page can still load
            _customStrategies = new List<CustomStrategyResult>();

            // Fire-and-forget notification (don't block page load on JSInterop)
            _ = ShowErrorAsync($"Failed to load custom strategies: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            Log("[StrategyLibrary] Load custom strategies complete");
        }
    }

    private void SetActiveTab(StrategyTab tab)
    {
        _activeTab = tab;
    }

    private string GetTabClass(StrategyTab tab)
    {
        bool isActive = _activeTab == tab;
        string baseClass = "py-4 px-1 border-b-2 font-medium text-sm transition-colors";

        if (isActive)
        {
            return $"{baseClass} border-blue-500 text-blue-600 dark:text-blue-400";
        }

        return $"{baseClass} border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 dark:text-gray-400 dark:hover:text-gray-300";
    }

    private void NavigateToBuilder()
    {
        Navigation.NavigateTo("/strategies/builder");
    }

    private void NavigateToEdit(int strategyId)
    {
        Navigation.NavigateTo($"/strategies/builder/{strategyId}");
    }

    private void NavigateToBacktest(string strategyType)
    {
        Navigation.NavigateTo($"/backtest?strategy={strategyType}");
    }

    private void NavigateToBacktestWithCustom(int strategyId)
    {
        Navigation.NavigateTo($"/backtest?customStrategyId={strategyId}");
    }

    private void NavigateToOptimize(int strategyId)
    {
        Navigation.NavigateTo($"/strategies/optimize?strategyId={strategyId}");
    }

    private async Task CloneStrategy(int strategyId)
    {
        try
        {
            CustomStrategyResult? original = _customStrategies.FirstOrDefault(s => s.Id == strategyId);
            if (original is null)
            {
                _ = ShowErrorAsync("Strategy not found");
                return;
            }

            string newName = $"{original.Name} (Copy)";
            Result<CustomStrategyResult> cloneResult = await CustomStrategyUseCase.CloneStrategyAsync(strategyId, newName);

            if (cloneResult.IsFailure)
            {
                _ = ShowErrorAsync(string.Join(", ", cloneResult.Errors.Select(e => e.Message)));
                return;
            }

            await LoadCustomStrategies();
            _ = ShowSuccessAsync($"Strategy cloned successfully as '{newName}'");
        }
        catch (Exception ex)
        {
            _ = ShowErrorAsync($"Failed to clone strategy: {ex.Message}");
        }
    }

    private void ShowDeleteDialog(int strategyId, string strategyName)
    {
        _strategyToDelete = strategyId;
        _strategyToDeleteName = strategyName;
        _showDeleteDialog = true;
    }

    private void CancelDelete()
    {
        _showDeleteDialog = false;
        _strategyToDelete = 0;
        _strategyToDeleteName = string.Empty;
    }

    private async Task ConfirmDelete()
    {
        _showDeleteDialog = false;

        try
        {
            Result<bool> deleteResult = await CustomStrategyUseCase.DeleteStrategyAsync(_strategyToDelete);

            if (deleteResult.IsFailure)
            {
                _ = ShowErrorAsync(string.Join(", ", deleteResult.Errors.Select(e => e.Message)));
                return;
            }

            await LoadCustomStrategies();
            _ = ShowSuccessAsync($"Strategy '{_strategyToDeleteName}' deleted successfully");
        }
        catch (Exception ex)
        {
            _ = ShowErrorAsync($"Failed to delete strategy: {ex.Message}");
        }
        finally
        {
            _strategyToDelete = 0;
            _strategyToDeleteName = string.Empty;
        }
    }

    private async Task ShowSuccessAsync(string message)
    {
        await NotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Success,
            "Success",
            message
        );
    }

    private async Task ShowErrorAsync(string message)
    {
        await NotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Error,
            "Error",
            message
        );
    }
}
