using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Goals.Events;

public sealed record GoalDeadlineRescheduled(
    GoalId   GoalId,
    DateOnly OldDeadline,
    DateOnly NewDeadline,
    DateTime OccurredAt) : DomainEvent(OccurredAt);
