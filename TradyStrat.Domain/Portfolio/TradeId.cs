namespace TradyStrat.Domain.Portfolio;

public readonly record struct TradeId(int Value)
{
    public static TradeId New() => new(0);
    public override string ToString() => $"TradeId({Value})";
}
