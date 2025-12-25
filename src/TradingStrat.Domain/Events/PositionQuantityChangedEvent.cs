using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.Events;

/// <summary>
/// Domain event raised when the quantity of an existing position changes.
/// </summary>
/// <param name="PortfolioId">The unique identifier of the portfolio.</param>
/// <param name="Ticker">The ticker symbol of the security.</param>
/// <param name="OldQuantity">The previous quantity.</param>
/// <param name="NewQuantity">The new quantity.</param>
public record PositionQuantityChangedEvent(
    int PortfolioId,
    string Ticker,
    int OldQuantity,
    int NewQuantity) : DomainEvent;
