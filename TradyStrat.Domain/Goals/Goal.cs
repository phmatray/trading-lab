using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Goals.Events;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed class Goal : AggregateRoot<GoalId>
{
    public Money    Target     { get; private set; } = Money.Zero(Currency.Eur);
    public DateOnly TargetDate { get; private set; } = DateOnly.MinValue;
    public DateTime UpdatedAt  { get; private set; }

    public bool HasDeadline => TargetDate != DateOnly.MinValue;

    private Goal() { }   // EF
    private Goal(GoalId id, Money target, DateOnly targetDate, DateTime updatedAt)
        : base(id)
    {
        Target     = target;
        TargetDate = targetDate;
        UpdatedAt  = updatedAt;
    }

    /// <summary>Singleton-id Initial Goal with €1M target and no deadline. Raises GoalCreated.</summary>
    public static Goal Initial(IClock clock)
    {
        var now = clock.UtcNow();
        var target = Money.Of(1_000_000m, Currency.Eur);
        var g = new Goal(GoalId.Singleton, target, DateOnly.MinValue, now);
        g.Raise(new GoalCreated(GoalId.Singleton, target, now));
        return g;
    }

    /// <summary>Rehydration factory used by EF mapping — does not raise.</summary>
    public static Goal Existing(GoalId id, Money target, DateOnly targetDate, DateTime updatedAt)
        => new(id, target, targetDate, updatedAt);

    public void RetargetAmount(Money newTarget, IClock clock)
    {
        if (newTarget.IsEmpty || newTarget.Amount <= 0m)
            throw new SettingValidationException(
                $"Target must be positive (was {newTarget}).");
        var oldTarget = Target;
        Target = newTarget;
        var now = clock.UtcNow();
        UpdatedAt = now;
        Raise(new GoalTargetChanged(Id, oldTarget, newTarget, now));
    }

    public void RescheduleDeadline(DateOnly newDeadline, IClock clock)
    {
        if (newDeadline != DateOnly.MinValue)
        {
            var today = clock.TodayLocal();
            if (newDeadline < today)
                throw new SettingValidationException(
                    $"Deadline must be today or later (was {newDeadline:O}, today is {today:O}).");
        }
        var oldDeadline = TargetDate;
        TargetDate = newDeadline;
        var now = clock.UtcNow();
        UpdatedAt = now;
        Raise(new GoalDeadlineRescheduled(Id, oldDeadline, newDeadline, now));
    }
}
