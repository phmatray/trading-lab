using TradyStrat.Domain;

namespace TradyStrat.Application.Indicators;

public interface IIndicatorEngine
{
    Task<IndicatorReading> ComputeFor(string ticker, CancellationToken ct);
    Task<IndicatorReading> ComputeFor(string ticker, DateOnly asOf, CancellationToken ct);
    Task<IndicatorSeries> HistoryFor(string ticker, IndicatorKind kind, int lastN, CancellationToken ct);
    Task<IndicatorSeries> HistoryFor(string ticker, IndicatorKind kind, int lastN, DateOnly asOf, CancellationToken ct);
}
