using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.Trades.Specifications;

public sealed class EarliestTradeSpec : Specification<Trade>
{
    public EarliestTradeSpec() => Query.OrderBy(t => t.ExecutedOn).Take(1);
}
