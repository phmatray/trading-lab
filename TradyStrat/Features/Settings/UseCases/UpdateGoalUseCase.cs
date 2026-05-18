using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using TradyStrat.Common.UseCases;
using TradyStrat.Data;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Features.Settings.UseCases;

public sealed record UpdateGoalInput(decimal TargetEur, DateOnly? TargetDate);

public sealed class UpdateGoalUseCase(
    IRepositoryBase<GoalConfig> repo, AppDbContext db, IClock clock,
    ILogger<UpdateGoalUseCase> log)
    : UseCaseBase<UpdateGoalInput, GoalConfig>(log)
{
    protected override async Task<GoalConfig> ExecuteCore(UpdateGoalInput input, CancellationToken ct)
    {
        if (input.TargetEur <= 0m)
            throw new TradeValidationException("Target must be positive.");

        var existing = await repo.GetByIdAsync(1, ct);
        var now = clock.UtcNow();

        if (existing is null)
        {
            var fresh = new GoalConfig
            {
                Id = 1,
                TargetEur = input.TargetEur,
                TargetDate = input.TargetDate,
                UpdatedAt = now,
            };
            await repo.AddAsync(fresh, ct);
            return fresh;
        }

        var updated = existing with
        {
            TargetEur = input.TargetEur,
            TargetDate = input.TargetDate,
            UpdatedAt = now,
        };

        // Detach the tracked instance so UpdateAsync can attach the updated copy without conflict.
        db.Entry(existing).State = EntityState.Detached;
        await repo.UpdateAsync(updated, ct);
        return updated;
    }
}
