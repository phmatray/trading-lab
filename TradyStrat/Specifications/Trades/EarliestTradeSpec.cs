using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Trades;

public sealed class EarliestTradeSpec : Specification<Trade>
{
    public EarliestTradeSpec() => Query.OrderBy(t => t.ExecutedOn).Take(1);
}
