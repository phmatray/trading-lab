using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Infrastructure.PriceFeed.Specifications;

public sealed class PriceBarsSinceSpec : Specification<PriceBar>
{
    public PriceBarsSinceSpec(string ticker, DateOnly since)
    {
        Query.Where(b => b.Ticker == ticker && b.Date >= since).OrderBy(b => b.Date);
    }
}
