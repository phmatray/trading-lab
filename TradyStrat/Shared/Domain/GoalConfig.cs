namespace TradyStrat.Shared.Domain;

public sealed record GoalConfig
{
    public required int Id { get; init; }
    public required decimal TargetEur { get; init; }
    public DateOnly? TargetDate { get; init; }
    public required string FocusTicker { get; init; }
    public required DateTime UpdatedAt { get; init; }

    public static GoalConfig Default(DateTime now) => new()
    {
        Id = 1,
        TargetEur = 1_000_000m,
        TargetDate = null,
        FocusTicker = "CON3.DE",
        UpdatedAt = now,
    };
}
