namespace TradyStrat.Domain;

public sealed record GoalConfig
{
    public required int Id { get; init; }
    public required decimal TargetEur { get; init; }
    public DateOnly? TargetDate { get; init; }
    public required DateTime UpdatedAt { get; init; }

    public static GoalConfig Default(DateTime now) => new()
    {
        Id = 1,
        TargetEur = 1_000_000m,
        TargetDate = null,
        UpdatedAt = now,
    };
}
