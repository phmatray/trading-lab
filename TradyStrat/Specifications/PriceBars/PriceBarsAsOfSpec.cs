using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.PriceBars;

public sealed class PriceBarsAsOfSpec : Specification<PriceBar>
{
    public PriceBarsAsOfSpec(string ticker, DateOnly asOfInclusive)
        => Query.Where(p => p.Ticker == ticker && p.Date <= asOfInclusive)
                .OrderBy(p => p.Date);
}
