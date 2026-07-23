using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for running strategy backtests on historical data.
/// Orchestrates historical data retrieval, strategy execution, and performance calculation.
/// Extracted from ProgramBacktest.RunAsync in original architecture.
/// </summary>
public interface IBacktestUseCase
{
    /// <summary>
    /// Executes a complete backtest of a trading strategy on historical data.
    /// </summary>
    /// <param name="command">Command containing strategy type, parameters, and backtest configuration.</param>
    /// <param name="progress">Optional progress reporter for UI updates during backtest execution.</param>
    /// <returns>Result containing complete backtest results including trades, performance metrics, and equity curve, or errors if the operation failed.</returns>
    Task<Result<BacktestResult>> ExecuteAsync(
        BacktestCommand command,
        IProgress<BacktestProgress>? progress = null);
}

/// <summary>
/// Command object for executing a backtest.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record BacktestCommand
{
    public string Ticker { get; init; }
    public StrategyType StrategyType { get; init; }
    public Dictionary<string, object>? StrategyParameters { get; init; }
    public decimal InitialCapital { get; init; }
    public decimal CommissionPercentage { get; init; }
    public decimal MinimumCommission { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public TimeFrame? TimeFrame { get; init; }
    public TradingStyle? TradingStyle { get; init; }
    public int? CustomStrategyId { get; init; }

    public BacktestCommand(
        string Ticker,
        StrategyType StrategyType,
        Dictionary<string, object>? StrategyParameters = null,
        decimal InitialCapital = 10000m,
        decimal CommissionPercentage = 0.001m,
        decimal MinimumCommission = 1.0m,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        TimeFrame? TimeFrame = null,
        TradingStyle? TradingStyle = null,
        int? CustomStrategyId = null)
    {
        // Validate parameters
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();
        ValidationGuard.Require(InitialCapital).GreaterThan(0m, "Initial capital must be positive");
        ValidationGuard.Require(CommissionPercentage).GreaterThanOrEqual(0m, "Commission percentage cannot be negative");
        ValidationGuard.Require(CommissionPercentage).LessThan(1m, "Commission percentage must be less than 100%");
        ValidationGuard.Require(MinimumCommission).GreaterThanOrEqual(0m, "Minimum commission cannot be negative");

        // Validate date range if both are provided
        if (StartDate.HasValue && EndDate.HasValue)
        {
            ValidationGuard.Require(StartDate.Value <= EndDate.Value,
                "Start date must be before or equal to end date",
                nameof(StartDate));
        }

        // Validate end date is not in the future
        if (EndDate.HasValue)
        {
            ValidationGuard.Require(EndDate.Value <= DateTime.Today,
                "End date cannot be in the future",
                nameof(EndDate));
        }

        // Validate CustomStrategyId if provided
        if (CustomStrategyId.HasValue)
        {
            ValidationGuard.Require(CustomStrategyId.Value).GreaterThan(0, "Custom strategy ID must be positive");
        }

        // Assign validated values
        this.Ticker = Ticker.ToUpperInvariant().Trim();
        this.StrategyType = StrategyType;
        this.StrategyParameters = StrategyParameters;
        this.InitialCapital = InitialCapital;
        this.CommissionPercentage = CommissionPercentage;
        this.MinimumCommission = MinimumCommission;
        this.StartDate = StartDate;
        this.EndDate = EndDate;
        this.TimeFrame = TimeFrame;
        this.TradingStyle = TradingStyle;
        this.CustomStrategyId = CustomStrategyId;
    }
}

/// <summary>
/// Progress update for backtest execution.
/// </summary>
/// <param name="Current">Current bar index being processed.</param>
/// <param name="Total">Total number of bars to process.</param>
/// <param name="Trades">Number of trades executed so far.</param>
public record BacktestProgress(
    int Current,
    int Total,
    int Trades);
