using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Infrastructure.PriceFeed.Specifications;

public sealed class LatestPriceBarSpec : Specification<PriceBar>
{
    public LatestPriceBarSpec(string ticker)
    {
        Query.Where(b => b.Ticker == ticker)
             .OrderByDescending(b => b.Date)
             .Take(1);
    }
}
