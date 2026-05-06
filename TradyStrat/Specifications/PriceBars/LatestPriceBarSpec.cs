using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.PriceBars;

public sealed class LatestPriceBarSpec : Specification<PriceBar>
{
    public LatestPriceBarSpec(string ticker)
    {
        Query.Where(b => b.Ticker == ticker)
             .OrderByDescending(b => b.Date)
             .Take(1);
    }
}
