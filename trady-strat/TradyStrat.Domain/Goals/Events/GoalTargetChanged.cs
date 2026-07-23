using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared.Money;

namespace TradyStrat.Domain.Goals.Events;

public sealed record GoalTargetChanged(
    GoalId   GoalId,
    Money    OldTarget,
    Money    NewTarget,
    DateTime OccurredAt) : DomainEvent(OccurredAt);
