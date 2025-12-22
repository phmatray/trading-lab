using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;

namespace TradingStrat.Web.Components.Pages;

public partial class StrategyLibrary
{
    [Inject]
    private ICustomStrategyManagementUseCase CustomStrategyUseCase { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadCustomStrategies();
    }

    private async Task LoadCustomStrategies()
    {
        isLoading = true;

        try
        {
            List<CustomStrategyResult> strategies = await CustomStrategyUseCase.GetAllStrategiesAsync();
            customStrategies = strategies.OrderByDescending(s => s.LastUpdatedAt).ToList();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to load custom strategies: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void SetActiveTab(StrategyTab tab)
    {
        activeTab = tab;
    }

    private string GetTabClass(StrategyTab tab)
    {
        bool isActive = activeTab == tab;
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
            CustomStrategyResult? original = customStrategies.FirstOrDefault(s => s.Id == strategyId);
            if (original == null)
            {
                await ShowErrorAsync("Strategy not found");
                return;
            }

            string newName = $"{original.Name} (Copy)";
            CustomStrategyResult cloned = await CustomStrategyUseCase.CloneStrategyAsync(strategyId, newName);

            await ShowSuccessAsync($"Strategy cloned successfully as '{newName}'");
            await LoadCustomStrategies();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to clone strategy: {ex.Message}");
        }
    }

    private async Task DeleteStrategy(int strategyId)
    {
        CustomStrategyResult? strategy = customStrategies.FirstOrDefault(s => s.Id == strategyId);
        if (strategy == null)
        {
            return;
        }

        bool confirmed = await JSRuntime.InvokeAsync<bool>(
            "confirm",
            $"Are you sure you want to delete '{strategy.Name}'? This action cannot be undone."
        );

        if (!confirmed)
        {
            return;
        }

        try
        {
            await CustomStrategyUseCase.DeleteStrategyAsync(strategyId);
            await ShowSuccessAsync($"Strategy '{strategy.Name}' deleted successfully");
            await LoadCustomStrategies();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to delete strategy: {ex.Message}");
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
