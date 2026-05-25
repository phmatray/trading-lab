using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.Goals;
using TradyStrat.Domain;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.Goals;

public sealed class EfGoalRepository(
    AppDbContext db,
    IClock clock,
    IDomainEventDispatcher dispatcher) : IGoalRepository
{
    public async Task<Goal> GetAsync(CancellationToken ct)
    {
        var goal = await db.Goals.SingleOrDefaultAsync(ct);
        if (goal is not null) return goal;

        // First-ever read: Initial(clock) raises GoalCreated. We persist, then
        // dispatch through the standard pipeline so any future handler observes
        // the first-install event.
        var initial = Goal.Initial(clock);
        db.Goals.Add(initial);
        await db.SaveChangesAsync(ct);
        await dispatcher.DispatchAsync(initial.DequeueDomainEvents(), ct);
        return initial;
    }

    public async Task<IReadOnlyList<IDomainEvent>> SaveAsync(Goal goal, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        return goal.DequeueDomainEvents();
    }
}
