using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Trades;

public sealed class AllTradesSpec : Specification<Trade>
{
    public AllTradesSpec() => Query.OrderBy(t => t.ExecutedOn).ThenBy(t => t.Id);
}
