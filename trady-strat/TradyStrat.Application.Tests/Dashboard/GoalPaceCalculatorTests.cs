using Shouldly;
using TradyStrat.Application.Dashboard;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared.Money;
using Xunit;

namespace TradyStrat.Application.Tests.Dashboard;

public class GoalPaceCalculatorTests
{
    private sealed class StubClock(DateTime now) : IClock
    {
        public DateTime UtcNow() => now;
        public DateOnly TodayLocal() => DateOnly.FromDateTime(now);
        public DateOnly TodayInExchangeTzFor(string ticker) => DateOnly.FromDateTime(now);
    }

    private static Goal Goal(decimal target = 500_000m, DateOnly? targetDate = null)
    {
        var deadline = targetDate ?? new DateOnly(2027, 6, 30);
        // Use a far-past clock so the deadline-must-be-future check passes.
        var clock = new StubClock(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var goal = global::TradyStrat.Domain.Goal.Initial(clock);
        goal.RetargetAmount(Money.Of(target, Currency.Eur), clock);
        goal.RescheduleDeadline(deadline, clock);
        return goal;
    }

    private static Goal GoalWithoutDeadline(decimal target = 500_000m)
    {
        var clock = new StubClock(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var goal = global::TradyStrat.Domain.Goal.Initial(clock);
        goal.RetargetAmount(Money.Of(target, Currency.Eur), clock);
        return goal;
    }

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
        var vm = GoalPaceCalculator.Compute(30_000m, GoalWithoutDeadline(),
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
