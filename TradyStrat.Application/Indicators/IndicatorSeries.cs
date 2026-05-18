namespace TradyStrat.Application.Indicators;

public sealed record IndicatorSeries(
    IReadOnlyList<decimal> Values,
    decimal? ThresholdHi,
    decimal? ThresholdLo)
{
    public static readonly IndicatorSeries Empty = new([], null, null);
}
