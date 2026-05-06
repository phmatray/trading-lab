using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Trades;

public sealed class LatestTradesSpec : Specification<Trade>
{
    public LatestTradesSpec(int count)
    {
        Query.OrderByDescending(t => t.ExecutedOn).ThenByDescending(t => t.Id).Take(count);
    }
}
