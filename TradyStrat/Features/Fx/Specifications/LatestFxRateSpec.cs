using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Fx.Specifications;

public sealed class LatestFxRateSpec : Specification<FxRate>
{
    public LatestFxRateSpec(string pair, DateOnly asOf)
    {
        Query.Where(r => r.Pair == pair && r.Date <= asOf)
             .OrderByDescending(r => r.Date)
             .Take(1);
    }
}
