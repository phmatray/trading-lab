using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Features.Trades.Specifications;

public sealed class TradesAsOfSpec : Specification<Trade>
{
    public TradesAsOfSpec(DateOnly asOfInclusive)
        => Query.Where(t => t.ExecutedOn <= asOfInclusive)
                .OrderBy(t => t.ExecutedOn);
}
