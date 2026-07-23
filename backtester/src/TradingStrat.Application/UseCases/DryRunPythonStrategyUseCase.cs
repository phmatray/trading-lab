using Microsoft.Extensions.Logging;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for dry-running a Python strategy on historical data without persisting it.
/// Validates syntax, fetches data, executes backtest, and returns simplified results.
/// Useful for testing Python code before saving the strategy.
/// </summary>
public class DryRunPythonStrategyUseCase
{
    private readonly IPythonExecutor _pythonExecutor;
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly BacktestEngine _backtestEngine;
    private readonly IIndicatorCalculator _indicatorCalculator;
    private readonly ILogger<DryRunPythonStrategyUseCase> _logger;

    public DryRunPythonStrategyUseCase(
        IPythonExecutor pythonExecutor,
        IHistoricalDataPort historicalDataPort,
        BacktestEngine backtestEngine,
        IIndicatorCalculator indicatorCalculator,
        ILogger<DryRunPythonStrategyUseCase> logger)
    {
        _pythonExecutor = pythonExecutor;
        _historicalDataPort = historicalDataPort;
        _backtestEngine = backtestEngine;
        _indicatorCalculator = indicatorCalculator;
        _logger = logger;
    }

    public async Task<Result<DryRunResult>> ExecuteAsync(DryRunPythonStrategyCommand command)
    {
        try
        {
            _logger.LogInformation("Dry-running Python strategy on {Ticker}", command.Ticker);

            // Step 1: Validate Python code syntax
            Domain.Services.PythonValidationResult validation = await _pythonExecutor.ValidateSyntaxAsync(command.PythonCode);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Python validation failed with {ErrorCount} errors", validation.Errors.Count);
                return Result<DryRunResult>.Success(new DryRunResult(
                    IsValid: false,
                    ValidationErrors: validation.Errors,
                    TotalTrades: null,
                    FinalEquity: null,
                    TotalReturn: null,
                    SharpeRatio: null
                ));
            }

            // Step 2: Fetch historical data
            List<HistoricalPrice> allData = await _historicalDataPort.GetHistoricalDataAsync(
                command.Ticker,
                TimeFrame.D1);

            if (allData.Count == 0)
            {
                return Result<DryRunResult>.Success(new DryRunResult(
                    IsValid: false,
                    ValidationErrors: new List<string> { $"No historical data found for {command.Ticker}. Please fetch data first." },
                    TotalTrades: null,
                    FinalEquity: null,
                    TotalReturn: null,
                    SharpeRatio: null
                ));
            }

            // Step 3: Determine date range
            DateTime? latestDate = await _historicalDataPort.GetLatestDataDateAsync(command.Ticker, TimeFrame.D1);
            DateTime endDate = command.EndDate ?? latestDate ?? DateTime.Today;
            DateTime startDate = command.StartDate ?? endDate.AddYears(-2);

            // Step 4: Create temporary Python strategy
            var strategy = new PythonScriptStrategy(
                _indicatorCalculator,
                _pythonExecutor,
                command.PythonCode,
                "Dry Run Strategy",
                "Temporary strategy for validation"
            );

            // Step 5: Create backtest configuration
            var config = new BacktestConfiguration(
                Ticker: command.Ticker,
                StartDate: startDate,
                EndDate: endDate,
                InitialCapital: command.InitialCash,
                CommissionPercentage: 0.001m,
                MinimumCommission: 1.0m,
                TimeFrame: TimeFrame.D1,
                TradingStyle: TradingStyle.LongTerm
            );

            // Step 6: Run backtest
            _logger.LogInformation("Running backtest for dry run on {Ticker} from {Start} to {End}",
                command.Ticker, startDate, endDate);

            BacktestResult result = await _backtestEngine.RunBacktestAsync(strategy, config, progress: null);

            // Step 7: Return simplified results
            DryRunResult dryRunResult = new(
                IsValid: true,
                ValidationErrors: new List<string>(),
                TotalTrades: result.Trades.Count,
                FinalEquity: result.Metrics.FinalEquity,
                TotalReturn: result.Metrics.TotalReturnPercentage,
                SharpeRatio: result.Metrics.SharpeRatio
            );

            _logger.LogInformation("Dry run completed: {Trades} trades, {Return:P2} return, Sharpe {Sharpe:F2}",
                dryRunResult.TotalTrades, dryRunResult.TotalReturn, dryRunResult.SharpeRatio);

            return Result<DryRunResult>.Success(dryRunResult);
        }
        catch (PythonExecutionException pex)
        {
            _logger.LogError(pex, "Python execution error during dry run");
            return Result<DryRunResult>.Success(new DryRunResult(
                IsValid: false,
                ValidationErrors: new List<string> { $"Python execution error: {pex.Message}" },
                TotalTrades: null,
                FinalEquity: null,
                TotalReturn: null,
                SharpeRatio: null
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dry-run Python strategy");
            return Result<DryRunResult>.Failure(
                Error.BusinessRule($"Dry run failed: {ex.Message}", ErrorCodes.Strategy.ExecutionFailed));
        }
    }
}
