using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.Goals;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.Goals;

public sealed class EfGoalRepository(AppDbContext db, IClock clock) : IGoalRepository
{
    public async Task<Goal> GetAsync(CancellationToken ct)
    {
        var goal = await db.Goals.SingleOrDefaultAsync(ct);
        if (goal is not null) return goal;

        var initial = Goal.Initial(clock);
        db.Goals.Add(initial);
        await db.SaveChangesAsync(ct);
        return initial;
    }

    public Task SaveAsync(Goal goal, CancellationToken ct) => db.SaveChangesAsync(ct);
}
