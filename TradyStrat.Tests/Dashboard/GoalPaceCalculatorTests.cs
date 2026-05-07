using Shouldly;
using TradyStrat.Features.Dashboard;
using TradyStrat.Common.Domain;
using Xunit;

namespace TradyStrat.Tests.Dashboard;

public class GoalPaceCalculatorTests
{
    private static GoalConfig Goal(decimal target = 500_000m, DateOnly? targetDate = null) => new()
    {
        Id = 1,
        TargetEur = target,
        TargetDate = targetDate ?? new DateOnly(2027, 6, 30),
        FocusTicker = "CON3.L",
        UpdatedAt = DateTime.UtcNow,
    };

    [Fact]
    public void NotStarted_when_firstTradeDate_null()
    {
        var vm = GoalPaceCalculator.Compute(
            currentValueEur: 30_000m, goal: Goal(),
            today: new DateOnly(2026, 5, 7), firstTradeDate: null);

        vm.Mode.ShouldBe(GoalPaceMode.NotStarted);
        vm.VsPlanEur.ShouldBe(0m);
        vm.MonthlyCompoundPct.ShouldBe(0m);
        vm.ImpliedCagrPct.ShouldBe(0m);
    }

    [Fact]
    public void NotStarted_when_no_target_date()
    {
        var goalNoDate = new GoalConfig
        {
            Id = 1, TargetEur = 500_000m, TargetDate = null,
            FocusTicker = "CON3.L", UpdatedAt = DateTime.UtcNow,
        };
        var vm = GoalPaceCalculator.Compute(30_000m, goalNoDate,
            new DateOnly(2026, 5, 7), new DateOnly(2026, 1, 1));
        vm.Mode.ShouldBe(GoalPaceMode.NotStarted);
    }

    [Fact]
    public void GoalDatePassed_when_today_after_target()
    {
        var vm = GoalPaceCalculator.Compute(30_000m, Goal(),
            today: new DateOnly(2027, 7, 1), firstTradeDate: new DateOnly(2026, 1, 1));

        vm.Mode.ShouldBe(GoalPaceMode.GoalDatePassed);
    }

    [Fact]
    public void TargetReached_when_currentValue_at_or_above_target()
    {
        var vm = GoalPaceCalculator.Compute(500_001m, Goal(),
            new DateOnly(2026, 5, 7), new DateOnly(2026, 1, 1));

        vm.Mode.ShouldBe(GoalPaceMode.TargetReached);
    }

    [Fact]
    public void Active_computes_vsPlan_negative_when_behind()
    {
        // first trade 2026-01-01, target 2027-06-30 → ~547 day plan.
        // today 2026-05-07 → ~127 days elapsed.
        // linear baseline = 500000 * (127 / 547) ≈ 116,000.
        // current 30,000 → vsPlan ≈ -86,000.
        var vm = GoalPaceCalculator.Compute(
            currentValueEur: 30_000m,
            goal: Goal(target: 500_000m, targetDate: new DateOnly(2027, 6, 30)),
            today: new DateOnly(2026, 5, 7),
            firstTradeDate: new DateOnly(2026, 1, 1));

        vm.Mode.ShouldBe(GoalPaceMode.Active);
        vm.VsPlanEur.ShouldBeLessThan(0m);
        vm.MonthlyCompoundPct.ShouldBeGreaterThan(0m);
        vm.ImpliedCagrPct.ShouldBeGreaterThan(0m);
    }
}
