using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Trades;

public sealed class TradesAsOfSpec : Specification<Trade>
{
    public TradesAsOfSpec(DateOnly asOfInclusive)
        => Query.Where(t => t.ExecutedOn <= asOfInclusive)
                .OrderBy(t => t.ExecutedOn);
}
