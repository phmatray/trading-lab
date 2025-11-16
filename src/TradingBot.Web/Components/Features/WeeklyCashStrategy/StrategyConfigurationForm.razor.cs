// <copyright file="StrategyConfigurationForm.razor.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;
using TradingBot.Web.Models;

namespace TradingBot.Web.Components.Features.WeeklyCashStrategy;

/// <summary>
/// Code-behind for StrategyConfigurationForm component.
/// Handles form logic for creating and updating weekly cash-managed strategies.
/// </summary>
public partial class StrategyConfigurationForm
{
    /// <summary>
    /// Gets or sets the strategy ID (null for new strategies).
    /// </summary>
    [Parameter]
    public Guid? StrategyId { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked after successful save.
    /// </summary>
    [Parameter]
    public EventCallback<Guid> OnSaved { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked on cancel.
    /// </summary>
    [Parameter]
    public EventCallback OnCancelled { get; set; }

    private StrategyConfigurationDto Configuration { get; set; } = new();

    private bool IsSaving { get; set; }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        if (StrategyId.HasValue)
        {
            await LoadExistingStrategy();
        }
        else
        {
            InitializeDefaults();
        }
    }

    private async Task LoadExistingStrategy()
    {
        try
        {
            var strategy = await WeeklyCashService.GetStrategyAsync(StrategyId!.Value);
            if (strategy != null)
            {
                Configuration = WeeklyCashService.ToConfigurationDto(strategy);
                Logger.LogInformation("Loaded strategy configuration: {StrategyId}", StrategyId);
            }
            else
            {
                Logger.LogWarning("Strategy not found: {StrategyId}", StrategyId);
                ToastService.ShowError("Strategy not found");
                await HandleCancel();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading strategy: {StrategyId}", StrategyId);
            ToastService.ShowError("Failed to load strategy configuration");
            await HandleCancel();
        }
    }

    private void InitializeDefaults()
    {
        Configuration = new StrategyConfigurationDto
        {
            Name = string.Empty,
            EtpSymbol = string.Empty,
            UnderlyingSymbol = string.Empty,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            WeeklyBuyRatio = 0.05m,
            WeeklySellRatio = 0.10m,
            ExecutionDayOfWeek = 5, // Friday
            IsBreakoutRuleEnabled = false,
            BreakoutPriceThreshold = 0.10m,
            BreakoutVolumeMultiplier = 1.5m,
            BreakoutBuyMultiplier = 2.0m,
        };
    }

    private async Task HandleSaveConfiguration()
    {
        IsSaving = true;

        try
        {
            Guid resultId;

            if (StrategyId.HasValue)
            {
                Logger.LogInformation("Updating strategy: {StrategyId}", StrategyId);
                await WeeklyCashService.UpdateStrategyAsync(StrategyId.Value, Configuration);
                resultId = StrategyId.Value;
                ToastService.ShowSuccess("Strategy configuration updated successfully");
            }
            else
            {
                Logger.LogInformation("Creating new strategy: {StrategyName}", Configuration.Name);
                resultId = await WeeklyCashService.CreateStrategyAsync(Configuration);
                ToastService.ShowSuccess("Strategy created successfully");
            }

            await OnSaved.InvokeAsync(resultId);
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning(ex, "Validation error saving strategy");
            ToastService.ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving strategy configuration");
            ToastService.ShowError("Failed to save strategy configuration. Please try again.");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task HandleCancel()
    {
        await OnCancelled.InvokeAsync();
    }
}
