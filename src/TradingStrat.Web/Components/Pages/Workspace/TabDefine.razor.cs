using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;

namespace TradingStrat.Web.Components.Pages.Workspace;

/// <summary>
/// Tab component for defining/selecting a custom strategy.
/// </summary>
public partial class TabDefine : ComponentBase
{
    #region Dependency Injection

    [Inject]
    private ICustomStrategyManagementUseCase CustomStrategyUseCase { get; set; } = default!;

    [Inject]
    private NotificationService Notifications { get; set; } = default!;

    #endregion

    #region Parameters

    /// <summary>
    /// Callback invoked when a strategy is created or selected.
    /// </summary>
    [Parameter]
    public EventCallback<CustomStrategy> OnStrategyCreated { get; set; }

    #endregion

    #region Private Fields

    private bool _showStrategyLibrary;
    private List<CustomStrategyResult>? _strategies;

    #endregion

    #region Event Handlers

    private async Task LoadExistingStrategy()
    {
        _showStrategyLibrary = true;

        try
        {
            _strategies = await CustomStrategyUseCase.GetAllStrategiesAsync();
        }
        catch (Exception ex)
        {
            await Notifications.AddNotificationAsync(
                NotificationType.System,
                NotificationSeverity.Error,
                "Load Failed",
                $"Failed to load strategies: {ex.Message}");
        }
    }

    private async Task SelectStrategy(int strategyId)
    {
        try
        {
            CustomStrategyResult strategyResult = await CustomStrategyUseCase.GetStrategyByIdAsync(strategyId);

            // Convert result to entity for workspace state
            CustomStrategy strategy = new()
            {
                Id = strategyResult.Id,
                Name = strategyResult.Name,
                Description = strategyResult.Description,
                Author = strategyResult.Author,
                Category = strategyResult.Category,
                DefinitionJson = System.Text.Json.JsonSerializer.Serialize(strategyResult.Definition),
                CreatedAt = strategyResult.CreatedAt,
                LastUpdatedAt = strategyResult.LastUpdatedAt,
                TimesUsed = strategyResult.TimesUsed,
                LastBacktestReturn = strategyResult.LastBacktestReturn,
                LastBacktestDate = strategyResult.LastBacktestDate
            };

            await OnStrategyCreated.InvokeAsync(strategy);
            await Notifications.AddNotificationAsync(
                NotificationType.System,
                NotificationSeverity.Success,
                "Strategy Loaded",
                $"Strategy '{strategy.Name}' loaded successfully");
        }
        catch (Exception ex)
        {
            await Notifications.AddNotificationAsync(
                NotificationType.System,
                NotificationSeverity.Error,
                "Load Failed",
                $"Failed to load strategy: {ex.Message}");
        }
    }

    #endregion
}
