using TradyStrat.Domain;

namespace TradyStrat.Application.Goals;

/// <summary>
/// Singleton-pattern repository for the Goal AR (only one row, GoalId.Singleton).
/// Mirrors IPortfolioRepository's shape.
/// </summary>
public interface IGoalRepository
{
    /// <summary>Returns the singleton Goal, creating it from Goal.Initial if not yet persisted.</summary>
    Task<Goal> GetAsync(CancellationToken ct);

    Task SaveAsync(Goal goal, CancellationToken ct);
}
