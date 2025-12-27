using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Strategies;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;

namespace TradingStrat.Web.Components.Pages.Workspace;

/// <summary>
/// Tab component for testing custom strategies via backtest execution.
/// </summary>
public partial class TabTest : ComponentBase
{
    #region Dependency Injection

    [Inject]
    private IBacktestUseCase BacktestUseCase { get; set; } = default!;

    [Inject]
    private NotificationService Notifications { get; set; } = default!;

    [Inject]
    private ProgressService ProgressService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    #endregion

    #region Parameters

    /// <summary>
    /// The custom strategy to test.
    /// </summary>
    [Parameter]
    public CustomStrategy? Strategy { get; set; }

    /// <summary>
    /// Callback invoked when test completes successfully.
    /// </summary>
    [Parameter]
    public EventCallback<BacktestResult> OnTestComplete { get; set; }

    #endregion

    #region Private Fields

    private readonly TestConfig _config = new();
    private BacktestResult? _result;
    private bool _isRunning;
    private string? _errorMessage;

    #endregion

    #region Lifecycle Methods

    protected override void OnParametersSet()
    {
        if (Strategy != null && string.IsNullOrEmpty(_config.Ticker))
        {
            // Initialize with defaults
            _config.Ticker = "AAPL";
            _config.StartDate = DateTime.Today.AddYears(-2);
            _config.EndDate = DateTime.Today;
        }
    }

    #endregion

    #region Event Handlers

    private async Task RunBacktest()
    {
        if (Strategy == null)
        {
            return;
        }

        _isRunning = true;
        _result = null;
        _errorMessage = null;

        try
        {
            BacktestCommand command = new(
                Ticker: _config.Ticker,
                StrategyType: StrategyType.RSI, // Placeholder
                StrategyParameters: new Dictionary<string, object>(),
                InitialCapital: _config.InitialCapital,
                CommissionPercentage: _config.CommissionPercentage,
                MinimumCommission: _config.MinimumCommission,
                StartDate: _config.StartDate,
                EndDate: _config.EndDate,
                TimeFrame: null,
                TradingStyle: null,
                CustomStrategyId: Strategy.Id
            );

            IProgress<BacktestProgress> progress = new Progress<BacktestProgress>(p =>
            {
                int percentage = p.Total > 0 ? (int)((double)p.Current / p.Total * 100) : 0;
                string message = $"Processing bar {p.Current}/{p.Total} ({p.Trades} trades executed)";
                ProgressService.UpdateProgress(message, percentage);
            });

            var backtestResult = await BacktestUseCase.ExecuteAsync(command, progress);

            if (backtestResult.IsSuccess)
            {
                _result = backtestResult.Value;

                await Notifications.AddNotificationAsync(
                    NotificationType.System,
                    NotificationSeverity.Success,
                    "Backtest Complete",
                    "Backtest completed successfully");
            }
            else
            {
                _errorMessage = string.Join(", ", backtestResult.Errors.Select(e => e.Message));
            }
        }
        catch (Exception ex)
        {
            await Notifications.AddNotificationAsync(
                NotificationType.System,
                NotificationSeverity.Error,
                "Backtest Failed",
                $"Backtest failed: {ex.Message}");
        }
        finally
        {
            _isRunning = false;
            ProgressService.Reset();
        }
    }

    #endregion
}
