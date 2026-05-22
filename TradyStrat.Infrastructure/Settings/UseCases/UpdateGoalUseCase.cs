using TradyStrat.Application.Goals;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Infrastructure.Settings.UseCases;

public sealed record UpdateGoalInput(decimal TargetEur, DateOnly? TargetDate);

public sealed class UpdateGoalUseCase(
    IGoalRepository repo,
    IClock clock,
    ILogger<UpdateGoalUseCase> log)
    : UseCaseBase<UpdateGoalInput, Goal>(log)
{
    protected override async Task<Goal> ExecuteCore(UpdateGoalInput input, CancellationToken ct)
    {
        var goal = await repo.GetAsync(ct);

        goal.RetargetAmount(Money.Of(input.TargetEur, Currency.Eur), clock);
        goal.RescheduleDeadline(input.TargetDate ?? DateOnly.MinValue, clock);

        await repo.SaveAsync(goal, ct);
        return goal;
    }
}
