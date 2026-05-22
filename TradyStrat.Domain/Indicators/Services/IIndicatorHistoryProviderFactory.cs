using TradyStrat.Domain;

namespace TradyStrat.Domain.Indicators.Services;

public interface IIndicatorHistoryProviderFactory
{
    IIndicatorHistoryProvider For(IndicatorKind kind);
}
