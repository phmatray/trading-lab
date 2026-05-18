using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.PriceFeed.Specifications;

public sealed class PriceBarsInRangeSpec : Specification<PriceBar>
{
    public PriceBarsInRangeSpec(string ticker, DateOnly from, DateOnly to)
    {
        Query
            .Where(b => b.Ticker == ticker && b.Date >= from && b.Date <= to)
            .OrderBy(b => b.Date);
    }
}
