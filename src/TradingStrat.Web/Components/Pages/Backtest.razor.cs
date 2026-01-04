using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Factories;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Strategies;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;
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
    [Inject] private ISaveBacktestRunUseCase SaveBacktestRunUseCase { get; set; } = null!;
    [Inject] private IStrategyFactory StrategyFactory { get; set; } = null!;
    [Inject] private IStrategyRegistry StrategyRegistry { get; set; } = null!;
    [Inject] private ICustomStrategyQueryUseCase CustomStrategyQueryUseCase { get; set; } = null!;
    [Inject] private UserPreferencesService PreferencesService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AppStateService AppState { get; set; } = null!;
    [Inject] private IOptions<TradingConfiguration> Configuration { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "strategy")]
    public string? QueryStrategy { get; set; }

    [SupplyParameterFromQuery(Name = "customStrategyId")]
    public int? QueryCustomStrategyId { get; set; }

    private readonly List<BreadcrumbNav.Breadcrumb> _breadcrumbs = new()
    {
        new() { Label = "Dashboard", Href = "/" },
        new() { Label = "Backtest", Href = "/backtest" }
    };

    private StrategyForm? _strategyForm;
    private CustomStrategyResult? _customStrategy;

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

            // Fetch custom strategy details
            Result<CustomStrategyResult> strategyResult = await CustomStrategyQueryUseCase.GetStrategyByIdAsync(QueryCustomStrategyId.Value);
            if (strategyResult.IsSuccess)
            {
                _customStrategy = strategyResult.Value;
            }
            else
            {
                ErrorMessage = $"Failed to load custom strategy: {string.Join(", ", strategyResult.Errors.Select(e => e.Message))}";
            }
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

    protected override async Task<Result<BacktestResult>> ExecuteOperationAsync(
        BacktestFormModel model,
        IProgress<string> progress)
    {
        // Start timing execution
        var stopwatch = Stopwatch.StartNew();

        // Get current strategy parameters from the form
        if (_strategyForm is not null)
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

        Result<BacktestResult> backtestResult = await BacktestUseCase.ExecuteAsync(command, backtestProgress);

        stopwatch.Stop();

        if (backtestResult.IsFailure)
        {
            return Result<BacktestResult>.Failure(backtestResult.Errors);
        }

        BacktestResult result = backtestResult.Value;

        // Auto-save backtest run to archive
        try
        {
            var saveCommand = new SaveBacktestRunCommand(
                Ticker: result.Ticker,
                StrategyType: model.StrategyType.ToString().ToLowerInvariant(),
                StrategyName: result.StrategyName,
                Config: new BacktestConfig(
                    ticker: model.Ticker,
                    startDate: model.StartDate ?? DateTime.Today.AddYears(-2),
                    endDate: model.EndDate ?? DateTime.Today,
                    initialCapital: model.InitialCapital,
                    commissionPercentage: model.CommissionPercentage,
                    minimumCommission: model.MinimumCommission
                ),
                Result: result,
                StrategyParameters: model.StrategyParameters,
                ExecutionTimeMs: (int)stopwatch.ElapsedMilliseconds,
                Status: "Success",
                ErrorMessage: null,
                Tags: null
            );

            Result<BacktestRun> saveResult = await SaveBacktestRunUseCase.ExecuteAsync(saveCommand);

            if (saveResult.IsFailure)
            {
                // Log but don't fail the backtest if archiving fails
                string errorMessage = string.Join(", ", saveResult.Errors.Select(e => e.Message));
                Console.WriteLine($"[WARNING] Failed to save backtest run to archive: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail the backtest if archiving fails
            Console.WriteLine($"[WARNING] Failed to save backtest run to archive: {ex.Message}");
        }

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

        // Save backtest context for quick actions
        var backtestContext = new Models.State.BacktestContext
        {
            Ticker = result.Ticker,
            StrategyName = result.StrategyName,
            StrategyParameters = model.StrategyParameters,
            Config = new BacktestConfig(
                ticker: model.Ticker,
                startDate: model.StartDate ?? DateTime.Today.AddYears(-2),
                endDate: model.EndDate ?? DateTime.Today,
                initialCapital: model.InitialCapital,
                commissionPercentage: model.CommissionPercentage,
                minimumCommission: model.MinimumCommission
            ),
            Result = result
        };
        await AppState.SetBacktestContextAsync(backtestContext);

        return Result<BacktestResult>.Success(result);
    }

    protected override string GetSuccessMessage(BacktestResult? result)
    {
        if (result is null)
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
        if (StrategyRegistry.TryParseStrategyType(value, out StrategyType strategyType))
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

    // Quick Actions navigation methods
    private void CreatePortfolioFromStrategy()
    {
        if (Result is null)
        {
            return;
        }

        // Navigate to portfolios page (backtest context already saved via AppState)
        Navigation.NavigateTo("/portfolios");
    }

    private void CompareWithOthers()
    {
        if (Result is null)
        {
            return;
        }

        // Navigate to strategy comparison page
        Navigation.NavigateTo("/strategies/compare");
    }

    private void OptimizeParameters()
    {
        if (Result is null)
        {
            return;
        }

        // Navigate to optimization page with current strategy as query parameter
        string strategyKey = GetStrategyKey(FormModel.StrategyType);
        Navigation.NavigateTo($"/strategies/optimize?strategy={strategyKey}");
    }

    private void ViewInArchive()
    {
        if (Result is null)
        {
            return;
        }

        // Navigate to backtest archive page
        Navigation.NavigateTo("/backtests");
    }
}
