using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio.Events;

public sealed record PortfolioCreated(PortfolioId PortfolioId, DateTime OccurredAt) : DomainEvent(OccurredAt);
