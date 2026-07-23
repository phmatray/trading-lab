namespace TradyStrat.Domain.Instruments;

public readonly record struct InstrumentId(int Value)
{
    public static InstrumentId New() => new(0);
    public override string ToString() => $"InstrumentId({Value})";
}
