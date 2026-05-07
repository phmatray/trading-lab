using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.PriceFeed.Specifications;

public sealed class LatestPriceBarSpec : Specification<PriceBar>
{
    public LatestPriceBarSpec(string ticker)
    {
        Query.Where(b => b.Ticker == ticker)
             .OrderByDescending(b => b.Date)
             .Take(1);
    }
}
