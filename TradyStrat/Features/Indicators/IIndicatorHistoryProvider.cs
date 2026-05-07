using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Indicators;

public interface IIndicatorHistoryProvider
{
    IndicatorKind Kind { get; }
    IndicatorSeries Compute(IReadOnlyList<PriceBar> bars, int lastN);
}
