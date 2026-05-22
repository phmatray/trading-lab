using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Infrastructure.Fx.Specifications;

public sealed class LatestFxRateSpec : Specification<FxRate>
{
    public LatestFxRateSpec(string @base, string quote, DateOnly asOf)
    {
        Query.Where(r => r.Pair.Base.Code == @base && r.Pair.Quote.Code == quote && r.Date <= asOf)
             .OrderByDescending(r => r.Date)
             .Take(1);
    }
}
