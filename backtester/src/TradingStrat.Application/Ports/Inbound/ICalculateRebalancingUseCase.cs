using TradingStrat.Application.Commands;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

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
    /// <returns>Result containing the rebalancing plan, or errors if the operation failed.</returns>
    Task<Result<RebalancingPlan>> ExecuteAsync(
        RebalancingCommand command,
        IProgress<string>? progress = null);
}
