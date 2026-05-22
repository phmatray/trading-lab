namespace TradyStrat.Domain.Shared;

public readonly record struct GoalId(int Value)
{
    public static GoalId New()      => new(0);
    public static GoalId Singleton => new(1);
    public override string ToString() => $"GoalId({Value})";
}
