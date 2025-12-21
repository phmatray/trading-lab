using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Command to calculate portfolio rebalancing.
/// </summary>
/// <param name="PortfolioId">The portfolio ID.</param>
/// <param name="TargetWeights">Target allocation weights.</param>
/// <param name="CommissionPercentage">Commission rate as decimal (e.g., 0.001 for 0.1%).</param>
/// <param name="MinimumCommission">Minimum commission amount.</param>
public record RebalancingCommand(
    int PortfolioId,
    AllocationWeights TargetWeights,
    decimal CommissionPercentage,
    decimal MinimumCommission
);

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
