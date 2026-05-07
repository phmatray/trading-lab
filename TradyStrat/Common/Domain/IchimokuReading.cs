namespace TradyStrat.Common.Domain;

public sealed record IchimokuReading(
    decimal Tenkan, decimal Kijun,
    decimal SenkouA, decimal SenkouB,
    decimal Chikou,
    IchimokuSignal Signal);
