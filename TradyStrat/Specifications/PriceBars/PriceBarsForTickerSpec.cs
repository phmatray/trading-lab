using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.PriceBars;

public sealed class PriceBarsForTickerSpec : Specification<PriceBar>
{
    public PriceBarsForTickerSpec(string ticker)
    {
        Query.Where(b => b.Ticker == ticker).OrderBy(b => b.Date);
    }
}
