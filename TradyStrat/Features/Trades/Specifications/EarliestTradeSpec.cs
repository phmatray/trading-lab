using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Trades.Specifications;

public sealed class EarliestTradeSpec : Specification<Trade>
{
    public EarliestTradeSpec() => Query.OrderBy(t => t.ExecutedOn).Take(1);
}
