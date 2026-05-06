using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Trades;

public sealed class TradesByDateRangeSpec : Specification<Trade>
{
    public TradesByDateRangeSpec(DateOnly from, DateOnly to)
    {
        Query.Where(t => t.ExecutedOn >= from && t.ExecutedOn <= to)
             .OrderBy(t => t.ExecutedOn);
    }
}
