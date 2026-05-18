using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.PriceFeed.Specifications;

public sealed class PriceBarsAsOfSpec : Specification<PriceBar>
{
    public PriceBarsAsOfSpec(string ticker, DateOnly asOfInclusive)
        => Query.Where(p => p.Ticker == ticker && p.Date <= asOfInclusive)
                .OrderBy(p => p.Date);
}
