using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Infrastructure.PriceFeed.Specifications;

public sealed class PriceBarsAsOfSpec : Specification<PriceBar>
{
    public PriceBarsAsOfSpec(string ticker, DateOnly asOfInclusive)
        => Query.Where(p => p.Ticker == ticker && p.Date <= asOfInclusive)
                .OrderBy(p => p.Date);
}
