using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Factories;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Strategies;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Strategies;
using TradingStrat.Web.Components.Shared;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Pages;

/// <summary>
/// Code-behind for Backtest page using BaseDataPage pattern.
/// </summary>
public partial class Backtest
{
    private const string FORM_KEY = "backtest-form";

    [Inject] private IBacktestUseCase BacktestUseCase { get; set; } = null!;
    [Inject] private IStrategyFactory StrategyFactory { get; set; } = null!;
    [Inject] private IStrategyRegistry StrategyRegistry { get; set; } = null!;
    [Inject] private UserPreferencesService PreferencesService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private IOptions<TradingConfiguration> Configuration { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "strategy")]
    public string? QueryStrategy { get; set; }

    [SupplyParameterFromQuery(Name = "customStrategyId")]
    public int? QueryCustomStrategyId { get; set; }

    private StrategyForm? _strategyForm;

    protected override string FormKey => FORM_KEY;

    protected override async Task<BacktestFormModel?> InitializeDefaultsAsync()
    {
        // Initialize from user preferences
        Models.State.UserPreferences prefs = await PreferencesService.GetPreferencesAsync();
        BacktestFormModel model = BacktestFormModel.FromPreferences(prefs, Configuration.Value, StrategyRegistry);

        // Apply query parameters if provided
        if (QueryCustomStrategyId.HasValue)
        {
            model.CustomStrategyId = QueryCustomStrategyId.Value;
        }
        else if (!string.IsNullOrEmpty(QueryStrategy))
        {
            if (StrategyRegistry.TryParseStrategyType(QueryStrategy, out StrategyType strategyType))
            {
                model.StrategyType = strategyType;
            }
        }

        return model;
    }

    protected override async Task<BacktestResult> ExecuteOperationAsync(
        BacktestFormModel model,
        IProgress<string> progress)
    {
        // Get current strategy parameters from the form
        if (_strategyForm != null)
        {
            model.StrategyParameters = _strategyForm.GetCurrentParameters();
        }

        // Convert string progress to BacktestProgress for the use case
        BacktestProgress? lastProgress = null;
        IProgress<BacktestProgress> backtestProgress = new Progress<BacktestProgress>(p =>
        {
            lastProgress = p;
            int percentage = p.Total > 0 ? (int)((double)p.Current / p.Total * 100) : 0;

            // Report with percentage (single update, no duplication)
            InvokeAsync(() => ProgressService.UpdateProgress(
                $"Processing bar {p.Current}/{p.Total} - {p.Trades} trades executed",
                percentage));
        });

        BacktestCommand command = new(
            model.Ticker,
            model.StrategyType,
            model.StrategyParameters,
            model.InitialCapital,
            model.CommissionPercentage,
            model.MinimumCommission,
            model.StartDate,
            model.EndDate,
            TimeFrame: null,  // Will default to D1 in use case
            TradingStyle: null,  // No trading style selected
            CustomStrategyId: model.CustomStrategyId
        );

        BacktestResult result = await BacktestUseCase.ExecuteAsync(command, backtestProgress);

        // Trigger backtest completion notification
        await NotificationService.AddNotificationAsync(
            NotificationType.Backtest,
            result.Metrics.TotalReturn >= 0
                ? NotificationSeverity.Success
                : NotificationSeverity.Warning,
            "Backtest Complete",
            $"{result.StrategyName} | {result.Trades.Count} trades | {result.Metrics.TotalReturnPercentage:+0.0;-0.0;0.0}% return",
            ticker: result.Ticker,
            action: new NotificationAction
            {
                Label = "View Results",
                TargetPage = "/backtest"
            }
        );

        return result;
    }

    protected override string GetSuccessMessage(BacktestResult? result)
    {
        if (result == null)
        {
            return "Backtest completed.";
        }

        return $"Backtest completed: {result.Trades.Count} trades executed, {result.Metrics.TotalReturnPercentage:+0.0;-0.0;0.0}% return";
    }

    private async Task OnFormFieldChanged()
        => await OnPropertyChangedAsync(_ => { }); // No-op update action, just saves state

    private async Task OnStrategyTypeChanged(string value)
    {
        // Parse strategy type string to enum
        if (StrategyRegistry.TryParseStrategyType(value, out var strategyType))
        {
            await OnPropertyChangedAsync(m => m.StrategyType = strategyType);
        }
    }

    private async Task OnStrategyParametersChanged(Dictionary<string, object> parameters)
        => await OnPropertyChangedAsync(m => m.StrategyParameters = parameters);

    private string GetStrategyType(string strategyName)
    {
        // Delegate to StrategyFactory for canonical strategy type mapping
        return StrategyFactory.MapStrategyNameToType(strategyName);
    }

    private string GetStrategyKey(StrategyType strategyType)
    {
        // Convert enum to string key for StrategyForm component
        return StrategyRegistry.GetDescriptor(strategyType).Key;
    }
}
