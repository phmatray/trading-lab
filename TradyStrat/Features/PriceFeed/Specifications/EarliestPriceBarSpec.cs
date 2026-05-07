using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.PriceFeed.Specifications;

public sealed class EarliestPriceBarSpec : Specification<PriceBar>
{
    public EarliestPriceBarSpec(string ticker)
    {
        Query.Where(b => b.Ticker == ticker)
             .OrderBy(b => b.Date)
             .Take(1);
    }
}
