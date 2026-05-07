using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Trades.Specifications;

public sealed class TradesByDateRangeSpec : Specification<Trade>
{
    public TradesByDateRangeSpec(DateOnly from, DateOnly to)
    {
        Query.Where(t => t.ExecutedOn >= from && t.ExecutedOn <= to)
             .OrderBy(t => t.ExecutedOn);
    }
}
