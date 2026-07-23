using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for running A/B parameter optimization tests between two strategy variants.
/// Orchestrates dual backtests and compares results using ranking criteria.
/// </summary>
public interface IParameterOptimizationUseCase
{
    /// <summary>
    /// Executes A/B testing by running two backtests and comparing results.
    /// </summary>
    /// <param name="command">Command containing two strategy variants to compare.</param>
    /// <param name="progress">Optional progress reporter for UI updates.</param>
    /// <returns>Result containing complete comparison results with ranking and metric breakdown, or errors if the operation failed.</returns>
    Task<Result<ParameterOptimizationResult>> ExecuteAsync(
        ParameterOptimizationCommand command,
        IProgress<OptimizationProgress>? progress = null);
}

/// <summary>
/// Command object for A/B parameter optimization.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record ParameterOptimizationCommand
{
    public string Ticker { get; init; }
    public StrategyVariant VariantA { get; init; }
    public StrategyVariant VariantB { get; init; }
    public decimal InitialCapital { get; init; }
    public decimal CommissionPercentage { get; init; }
    public decimal MinimumCommission { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public TimeFrame? TimeFrame { get; init; }

    public ParameterOptimizationCommand(
        string Ticker,
        StrategyVariant VariantA,
        StrategyVariant VariantB,
        decimal InitialCapital = 10000m,
        decimal CommissionPercentage = 0.001m,
        decimal MinimumCommission = 1.0m,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        TimeFrame? TimeFrame = null)
    {
        // Validate parameters
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();
        ValidationGuard.Require(VariantA).NotNull();
        ValidationGuard.Require(VariantB).NotNull();
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

        // Assign validated values
        this.Ticker = Ticker.ToUpperInvariant().Trim();
        this.VariantA = VariantA;
        this.VariantB = VariantB;
        this.InitialCapital = InitialCapital;
        this.CommissionPercentage = CommissionPercentage;
        this.MinimumCommission = MinimumCommission;
        this.StartDate = StartDate;
        this.EndDate = EndDate;
        this.TimeFrame = TimeFrame;
    }
}

/// <summary>
/// Result object containing comparison between two strategy variants.
/// </summary>
public record ParameterOptimizationResult(
    StrategyComparison Comparison,
    TimeSpan ExecutionTime);

/// <summary>
/// Progress update for optimization execution.
/// </summary>
public record OptimizationProgress(
    string CurrentVariant,  // "Variant A" or "Variant B"
    int CurrentBar,
    int TotalBars,
    int Trades);
