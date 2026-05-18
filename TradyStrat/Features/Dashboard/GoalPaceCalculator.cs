using TradyStrat.Domain;

namespace TradyStrat.Features.Dashboard;

public enum GoalPaceMode { Active, NotStarted, GoalDatePassed, TargetReached }

public sealed record GoalPaceVm(
    decimal VsPlanEur,
    decimal MonthlyCompoundPct,
    decimal ImpliedCagrPct,
    GoalPaceMode Mode)
{
    public static readonly GoalPaceVm Zero = new(0m, 0m, 0m, GoalPaceMode.NotStarted);
}

public static class GoalPaceCalculator
{
    public static GoalPaceVm Compute(
        decimal currentValueEur,
        GoalConfig goal,
        DateOnly today,
        DateOnly? firstTradeDate)
    {
        if (firstTradeDate is null || goal.TargetDate is null)
            return GoalPaceVm.Zero;

        var targetDate = goal.TargetDate.Value;
        if (today > targetDate)
            return new(0m, 0m, 0m, GoalPaceMode.GoalDatePassed);

        if (currentValueEur >= goal.TargetEur)
            return new(currentValueEur - goal.TargetEur, 0m, 0m, GoalPaceMode.TargetReached);

        var totalPlanDays = targetDate.DayNumber - firstTradeDate.Value.DayNumber;
        var elapsedDays   = today.DayNumber - firstTradeDate.Value.DayNumber;
        if (totalPlanDays <= 0 || elapsedDays < 0)
            return GoalPaceVm.Zero;

        var baseline   = goal.TargetEur * elapsedDays / totalPlanDays;
        var vsPlan     = currentValueEur - baseline;

        var daysLeft   = targetDate.DayNumber - today.DayNumber;
        var monthsLeft = daysLeft / 30m;
        var yearsLeft  = daysLeft / 365m;

        decimal monthlyPct = 0m, cagrPct = 0m;
        if (monthsLeft > 0m && currentValueEur > 0m)
        {
            var ratio = (double)(goal.TargetEur / currentValueEur);
            monthlyPct = (decimal)(Math.Pow(ratio, 1.0 / (double)monthsLeft) - 1.0) * 100m;
            cagrPct    = (decimal)(Math.Pow(ratio, 1.0 / (double)yearsLeft)  - 1.0) * 100m;
        }

        return new(vsPlan, monthlyPct, cagrPct, GoalPaceMode.Active);
    }
}
