using TradyStrat.Domain;

namespace TradyStrat.Application.Indicators.History;

public interface IIndicatorHistoryProvider
{
    IndicatorKind Kind { get; }
    IndicatorSeries Compute(IReadOnlyList<PriceBar> bars, int lastN);
}
