namespace TradyStrat.Domain.Portfolio;

public readonly record struct PositionId(int Value)
{
    public static PositionId New() => new(0);
    public override string ToString() => $"PositionId({Value})";
}
