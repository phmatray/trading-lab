using TradyStrat.Domain;

namespace TradyStrat.Application.Fx.Providers;

public interface IFxRateProvider
{
    Task<IReadOnlyList<FxRate>> FetchAsync(
        string @base, string quote, DateOnly from, DateOnly to, CancellationToken ct);
}
