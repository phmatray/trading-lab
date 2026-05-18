using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.Fx.Specifications;

public sealed class LatestFxRateSpec : Specification<FxRate>
{
    public LatestFxRateSpec(string @base, string quote, DateOnly asOf)
    {
        Query.Where(r => r.Base == @base && r.Quote == quote && r.Date <= asOf)
             .OrderByDescending(r => r.Date)
             .Take(1);
    }
}
