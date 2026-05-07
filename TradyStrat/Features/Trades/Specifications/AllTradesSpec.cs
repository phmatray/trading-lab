using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Trades.Specifications;

public sealed class AllTradesSpec : Specification<Trade>
{
    public AllTradesSpec() => Query.OrderBy(t => t.ExecutedOn).ThenBy(t => t.Id);
}
