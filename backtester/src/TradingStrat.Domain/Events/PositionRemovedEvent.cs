using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.Events;

/// <summary>
/// Domain event raised when a position is removed from a portfolio.
/// </summary>
/// <param name="PortfolioId">The unique identifier of the portfolio.</param>
/// <param name="Ticker">The ticker symbol of the security.</param>
/// <param name="Quantity">The number of shares that were removed.</param>
public record PositionRemovedEvent(
    int PortfolioId,
    string Ticker,
    int Quantity) : DomainEvent;
