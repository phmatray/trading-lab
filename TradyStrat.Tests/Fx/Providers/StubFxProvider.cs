using TradyStrat.Application.Fx.Providers;
using TradyStrat.Domain;

namespace TradyStrat.Tests.Fx.Providers;

public sealed class StubFxProvider(IReadOnlyList<FxRate> rates) : IFxRateProvider
{
    public int CallCount { get; private set; }

    public Task<IReadOnlyList<FxRate>> FetchAsync(
        string @base, string quote, DateOnly from, DateOnly to, CancellationToken ct)
    {
        CallCount++;
        return Task.FromResult(rates);
    }
}
