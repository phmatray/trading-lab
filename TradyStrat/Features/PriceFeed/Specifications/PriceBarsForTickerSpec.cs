using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.PriceFeed.Specifications;

public sealed class PriceBarsForTickerSpec : Specification<PriceBar>
{
    public PriceBarsForTickerSpec(string ticker)
    {
        Query.Where(b => b.Ticker == ticker).OrderBy(b => b.Date);
    }
}
