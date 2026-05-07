using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Indicators;

public interface IIndicatorHistoryProviderFactory
{
    IIndicatorHistoryProvider For(IndicatorKind kind);
}
