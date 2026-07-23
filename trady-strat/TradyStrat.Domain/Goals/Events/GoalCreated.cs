using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared.Money;

namespace TradyStrat.Domain.Goals.Events;

public sealed record GoalCreated(GoalId GoalId, Money Target, DateTime OccurredAt) : DomainEvent(OccurredAt);
