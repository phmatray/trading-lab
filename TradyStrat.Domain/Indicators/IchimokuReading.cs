namespace TradyStrat.Domain;

public sealed record IchimokuReading(
    decimal Tenkan, decimal Kijun,
    decimal SenkouA, decimal SenkouB,
    decimal Chikou,
    IchimokuSignal Signal)
{
    // None signal is reserved for "no reading available" — distinguishes
    // a real InCloud reading from a missing one.
    public static readonly IchimokuReading Empty = new(0m, 0m, 0m, 0m, 0m, IchimokuSignal.None);
    public bool IsEmpty => Signal == IchimokuSignal.None;
}
