using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.Events;

/// <summary>
/// Domain event raised when a new portfolio is created.
/// </summary>
/// <param name="PortfolioId">The unique identifier of the portfolio.</param>
/// <param name="Name">The name of the portfolio.</param>
/// <param name="InitialCash">The initial cash balance.</param>
public record PortfolioCreatedEvent(
    int PortfolioId,
    string Name,
    decimal InitialCash) : DomainEvent;
