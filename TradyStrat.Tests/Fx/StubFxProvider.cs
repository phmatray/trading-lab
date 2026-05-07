using TradyStrat.Features.Fx.Providers;
using TradyStrat.Common.Domain;

namespace TradyStrat.Tests.Fx;

public sealed class StubFxProvider(IReadOnlyList<FxRate> rates) : IFxRateProvider
{
    public int CallCount { get; private set; }

    public Task<IReadOnlyList<FxRate>> FetchAsync(
        string pair, DateOnly from, DateOnly to, CancellationToken ct)
    {
        CallCount++;
        return Task.FromResult(rates);
    }
}
