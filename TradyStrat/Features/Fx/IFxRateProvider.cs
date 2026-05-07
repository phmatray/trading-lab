using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Fx;

public interface IFxRateProvider
{
    Task<IReadOnlyList<FxRate>> FetchAsync(
        string pair, DateOnly from, DateOnly to, CancellationToken ct);
}
