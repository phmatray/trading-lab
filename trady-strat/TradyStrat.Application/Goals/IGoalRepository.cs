using TradyStrat.Domain;
using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Application.Goals;

/// <summary>
/// Singleton-pattern repository for the Goal AR (only one row, GoalId.Singleton).
/// </summary>
public interface IGoalRepository
{
    Task<Goal> GetAsync(CancellationToken ct);
    Task<IReadOnlyList<IDomainEvent>> SaveAsync(Goal goal, CancellationToken ct);
}
