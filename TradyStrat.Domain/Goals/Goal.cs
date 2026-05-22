using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed class Goal
{
    public GoalId   Id         { get; private set; }
    public Money    Target     { get; private set; } = Money.Zero(Currency.Eur);
    public DateOnly TargetDate { get; private set; } = DateOnly.MinValue;
    public DateTime UpdatedAt  { get; private set; }

    public bool HasDeadline => TargetDate != DateOnly.MinValue;

    private Goal() { }   // EF

    private Goal(GoalId id, Money target, DateOnly targetDate, DateTime updatedAt)
    {
        Id         = id;
        Target     = target;
        TargetDate = targetDate;
        UpdatedAt  = updatedAt;
    }

    /// <summary>Singleton-id Initial Goal with €1M target and no deadline.</summary>
    public static Goal Initial(IClock clock)
        => new(GoalId.Singleton,
               Money.Of(1_000_000m, Currency.Eur),
               DateOnly.MinValue,
               clock.UtcNow());

    /// <summary>Rehydration factory used by EF mapping.</summary>
    public static Goal Existing(GoalId id, Money target, DateOnly targetDate, DateTime updatedAt)
        => new(id, target, targetDate, updatedAt);

    public void RetargetAmount(Money newTarget, IClock clock)
    {
        if (newTarget.IsEmpty || newTarget.Amount <= 0m)
            throw new SettingValidationException(
                $"Target must be positive (was {newTarget}).");
        Target = newTarget;
        UpdatedAt = clock.UtcNow();
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
        TargetDate = newDeadline;
        UpdatedAt = clock.UtcNow();
    }
}
