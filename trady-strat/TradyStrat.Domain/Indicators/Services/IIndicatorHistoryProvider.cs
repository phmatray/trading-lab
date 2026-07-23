using TradyStrat.Domain;
using TradyStrat.Domain.Indicators;

namespace TradyStrat.Domain.Indicators.Services;

public interface IIndicatorHistoryProvider
{
    IndicatorKind Kind { get; }
    IndicatorSeries Compute(IReadOnlyList<PriceBar> bars, int lastN);
}
