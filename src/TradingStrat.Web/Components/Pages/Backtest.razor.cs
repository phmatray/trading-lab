using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Entities;
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
    [Inject] private UserPreferencesService PreferencesService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private IOptions<TradingConfiguration> Configuration { get; set; } = null!;

    private StrategyForm? _strategyForm;

    protected override string FormKey => FORM_KEY;

    protected override async Task<BacktestFormModel?> InitializeDefaultsAsync()
    {
        // Initialize from user preferences
        Models.State.UserPreferences prefs = await PreferencesService.GetPreferencesAsync();
        return BacktestFormModel.FromPreferences(prefs, Configuration.Value);
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
            progress.Report($"Processing bar {p.Current}/{p.Total} - {p.Trades} trades executed");

            // Update progress service directly for percentage
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
            model.EndDate
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
    {
        await FormState.SaveFormStateAsync(FormKey, FormModel);
    }

    private async Task OnStrategyTypeChanged(string value)
    {
        FormModel.StrategyType = value;
        await OnFormFieldChanged();
    }

    private async Task OnStrategyParametersChanged(Dictionary<string, object> parameters)
    {
        FormModel.StrategyParameters = parameters;
        await OnFormFieldChanged();
    }

    private static string GetStrategyType(string strategyName)
    {
        // Map strategy names to types: "Moving Average Crossover" -> "ma"
        return strategyName.ToLowerInvariant() switch
        {
            var s when s.Contains("moving average") => "ma",
            var s when s.Contains("rsi") => "rsi",
            var s when s.Contains("macd") => "macd",
            var s when s.Contains("machine learning") || s.Contains("ml") => "ml",
            var s when s.Contains("ichimoku") => "ichimoku",
            _ => "ma"
        };
    }
}
