using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Goals.Events;

public sealed record GoalDeadlineRescheduled(
    GoalId   GoalId,
    DateOnly OldDeadline,
    DateOnly NewDeadline,
    DateTime OccurredAt) : DomainEvent(OccurredAt);
