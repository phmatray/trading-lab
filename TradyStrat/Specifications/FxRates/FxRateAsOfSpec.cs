using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.FxRates;

public sealed class FxRateAsOfSpec : Specification<FxRate>
{
    public FxRateAsOfSpec(string pair, DateOnly asOfInclusive)
        => Query.Where(f => f.Pair == pair && f.Date <= asOfInclusive)
                .OrderByDescending(f => f.Date)
                .Take(1);
}
