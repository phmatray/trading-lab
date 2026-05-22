namespace TradyStrat.Domain.Shared;

public readonly record struct InstrumentId(int Value)
{
    public static InstrumentId New() => new(0);
    public override string ToString() => $"InstrumentId({Value})";
}
