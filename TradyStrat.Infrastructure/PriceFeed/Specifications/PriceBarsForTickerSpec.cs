using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Infrastructure.PriceFeed.Specifications;

public sealed class PriceBarsForTickerSpec : Specification<PriceBar>
{
    public PriceBarsForTickerSpec(string ticker)
    {
        Query.Where(b => b.Ticker == ticker).OrderBy(b => b.Date);
    }
}
