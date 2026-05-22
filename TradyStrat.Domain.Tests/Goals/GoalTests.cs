using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Goals;

public class GoalTests
{
    private static readonly DateTime _now = new(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime UtcNow() => now;
        public DateOnly TodayLocal() => DateOnly.FromDateTime(now);
        public DateOnly TodayInExchangeTzFor(string ticker) => DateOnly.FromDateTime(now);
    }

    [Fact]
    public void Initial_uses_singleton_id_and_default_one_million_target_no_deadline()
    {
        var goal = Goal.Initial(new FixedClock(_now));
        goal.Id.ShouldBe(GoalId.Singleton);
        goal.Target.ShouldBe(Money.Of(1_000_000m, Currency.Eur));
        goal.HasDeadline.ShouldBeFalse();
        goal.UpdatedAt.ShouldBe(_now);
    }

    [Fact]
    public void RetargetAmount_updates_Target_and_UpdatedAt_only()
    {
        var goal = Goal.Initial(new FixedClock(_now));
        var clock2 = new FixedClock(_now.AddDays(1));

        goal.RetargetAmount(Money.Of(2_000_000m, Currency.Eur), clock2);

        goal.Target.ShouldBe(Money.Of(2_000_000m, Currency.Eur));
        goal.HasDeadline.ShouldBeFalse();
        goal.UpdatedAt.ShouldBe(clock2.UtcNow());
    }

    [Fact]
    public void RetargetAmount_rejects_non_positive_target()
    {
        var goal = Goal.Initial(new FixedClock(_now));
        Should.Throw<SettingValidationException>(
            () => goal.RetargetAmount(Money.Of(0m, Currency.Eur), new FixedClock(_now)));
    }

    [Fact]
    public void RescheduleDeadline_sets_HasDeadline_true_for_future_date()
    {
        var goal = Goal.Initial(new FixedClock(_now));
        var future = DateOnly.FromDateTime(_now).AddDays(90);

        goal.RescheduleDeadline(future, new FixedClock(_now));

        goal.HasDeadline.ShouldBeTrue();
        goal.TargetDate.ShouldBe(future);
    }

    [Fact]
    public void RescheduleDeadline_with_MinValue_clears_HasDeadline()
    {
        var goal = Goal.Initial(new FixedClock(_now));
        goal.RescheduleDeadline(DateOnly.FromDateTime(_now).AddDays(90), new FixedClock(_now));

        goal.RescheduleDeadline(DateOnly.MinValue, new FixedClock(_now.AddDays(1)));

        goal.HasDeadline.ShouldBeFalse();
        goal.TargetDate.ShouldBe(DateOnly.MinValue);
    }

    [Fact]
    public void RescheduleDeadline_rejects_past_dates()
    {
        var clock = new FixedClock(_now);
        var goal = Goal.Initial(clock);
        var yesterday = DateOnly.FromDateTime(_now).AddDays(-1);

        Should.Throw<SettingValidationException>(
            () => goal.RescheduleDeadline(yesterday, clock));
    }
}
