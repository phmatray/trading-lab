using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Command to calculate portfolio rebalancing.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record RebalancingCommand
{
    public int PortfolioId { get; init; }
    public AllocationWeights TargetWeights { get; init; }
    public decimal CommissionPercentage { get; init; }
    public decimal MinimumCommission { get; init; }

    public RebalancingCommand(
        int PortfolioId,
        AllocationWeights TargetWeights,
        decimal CommissionPercentage,
        decimal MinimumCommission)
    {
        // Validate parameters
        ValidationGuard.Require(PortfolioId).GreaterThan(0, "Portfolio ID must be positive");
        ValidationGuard.Require(TargetWeights).NotNull();
        ValidationGuard.Require(CommissionPercentage).GreaterThanOrEqual(0m, "Commission percentage cannot be negative");
        ValidationGuard.Require(CommissionPercentage).LessThan(1m, "Commission percentage must be less than 100%");
        ValidationGuard.Require(MinimumCommission).GreaterThanOrEqual(0m, "Minimum commission cannot be negative");

        // Assign validated values
        this.PortfolioId = PortfolioId;
        this.TargetWeights = TargetWeights;
        this.CommissionPercentage = CommissionPercentage;
        this.MinimumCommission = MinimumCommission;
    }
}

/// <summary>
/// Inbound port (use case interface) for calculating portfolio rebalancing.
/// </summary>
public interface ICalculateRebalancingUseCase
{
    /// <summary>
    /// Calculates a rebalancing plan for a portfolio.
    /// </summary>
    /// <param name="command">The rebalancing command.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>The rebalancing plan.</returns>
    Task<RebalancingPlan> ExecuteAsync(
        RebalancingCommand command,
        IProgress<string>? progress = null);
}
