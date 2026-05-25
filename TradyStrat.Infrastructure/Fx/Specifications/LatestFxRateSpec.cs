using Ardalis.Specification;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared.Money;

namespace TradyStrat.Infrastructure.Fx.Specifications;

public sealed class LatestFxRateSpec : Specification<FxRate>
{
    public LatestFxRateSpec(string @base, string quote, DateOnly asOf)
    {
        var b = Currency.Parse(@base);
        var q = Currency.Parse(quote);
        Query.Where(r => r.Pair.Base == b && r.Pair.Quote == q && r.Date <= asOf)
             .OrderByDescending(r => r.Date)
             .Take(1);
    }
}
