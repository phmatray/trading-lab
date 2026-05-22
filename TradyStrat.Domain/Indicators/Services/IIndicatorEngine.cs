using TradyStrat.Domain;
using TradyStrat.Domain.Indicators;

namespace TradyStrat.Domain.Indicators.Services;

public interface IIndicatorEngine
{
    Task<IndicatorReading> ComputeFor(string ticker, CancellationToken ct);
    Task<IndicatorReading> ComputeFor(string ticker, DateOnly asOf, CancellationToken ct);
    Task<IndicatorSeries> HistoryFor(string ticker, IndicatorKind kind, int lastN, CancellationToken ct);
    Task<IndicatorSeries> HistoryFor(string ticker, IndicatorKind kind, int lastN, DateOnly asOf, CancellationToken ct);
}
