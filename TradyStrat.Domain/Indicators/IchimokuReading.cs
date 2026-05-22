namespace TradyStrat.Domain;

public sealed record IchimokuReading(
    decimal Tenkan, decimal Kijun,
    decimal SenkouA, decimal SenkouB,
    decimal Chikou,
    IchimokuSignal Signal)
{
    // InCloud is the natural "no clear signal" default — used as the Empty
    // sentinel when the price series is too short to compute the reading.
    public static readonly IchimokuReading Empty = new(0m, 0m, 0m, 0m, 0m, IchimokuSignal.InCloud);
    public bool IsEmpty => this == Empty;
}
