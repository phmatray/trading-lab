using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.Events;

/// <summary>
/// Domain event raised when a portfolio rebalancing operation is completed.
/// </summary>
/// <param name="PortfolioId">The unique identifier of the portfolio.</param>
/// <param name="SignalsExecutedCount">The number of rebalancing signals that were executed.</param>
/// <param name="TotalCostIncludingCommissions">The total cost of the rebalancing operation including commissions.</param>
public record PortfolioRebalancedEvent(
    int PortfolioId,
    int SignalsExecutedCount,
    decimal TotalCostIncludingCommissions) : DomainEvent;
