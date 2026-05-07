using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Fx.Providers;

public interface IFxRateProvider
{
    Task<IReadOnlyList<FxRate>> FetchAsync(
        string pair, DateOnly from, DateOnly to, CancellationToken ct);
}
