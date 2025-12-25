using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.Events;

/// <summary>
/// Domain event raised when a position is added to a portfolio.
/// </summary>
/// <param name="PortfolioId">The unique identifier of the portfolio.</param>
/// <param name="Ticker">The ticker symbol of the security.</param>
/// <param name="Quantity">The number of shares.</param>
/// <param name="EntryPrice">The price per share at entry.</param>
/// <param name="EntryDate">The date the position was entered.</param>
public record PositionAddedEvent(
    int PortfolioId,
    string Ticker,
    int Quantity,
    decimal EntryPrice,
    DateTime EntryDate) : DomainEvent;
