using TradyStrat.Domain;

namespace TradyStrat.Application.Indicators.History;

public interface IIndicatorHistoryProviderFactory
{
    IIndicatorHistoryProvider For(IndicatorKind kind);
}
