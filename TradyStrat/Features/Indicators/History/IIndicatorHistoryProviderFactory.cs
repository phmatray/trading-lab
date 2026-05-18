using TradyStrat.Domain;

namespace TradyStrat.Features.Indicators.History;

public interface IIndicatorHistoryProviderFactory
{
    IIndicatorHistoryProvider For(IndicatorKind kind);
}
