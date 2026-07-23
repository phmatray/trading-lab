using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Portfolio.Events;

public sealed record PortfolioCreated(PortfolioId PortfolioId, DateTime OccurredAt) : DomainEvent(OccurredAt);
