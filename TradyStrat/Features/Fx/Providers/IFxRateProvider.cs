using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Fx.Providers;

public interface IFxRateProvider
{
    Task<IReadOnlyList<FxRate>> FetchAsync(
        string @base, string quote, DateOnly from, DateOnly to, CancellationToken ct);
}
