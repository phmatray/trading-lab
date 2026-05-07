using Ardalis.Specification;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Features.Fx.Specifications;

namespace TradyStrat.Features.Fx;

public sealed class FxConverter(IReadRepositoryBase<FxRate> rates)
{
    public async Task<decimal> UsdToEurAsync(decimal usd, DateOnly asOf, CancellationToken ct)
    {
        var fx = await rates.FirstOrDefaultAsync(new LatestFxRateSpec("EURUSD", asOf), ct)
            ?? throw new FxRateUnavailableException(
                $"No EURUSD rate on or before {asOf:yyyy-MM-dd}");
        return usd * fx.EurPerUsd;
    }
}
