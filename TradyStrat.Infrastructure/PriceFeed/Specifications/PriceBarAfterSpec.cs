using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Infrastructure.PriceFeed.Specifications;

public sealed class PriceBarAfterSpec : Specification<PriceBar>
{
    public PriceBarAfterSpec(string ticker, DateOnly exclusive)
    {
        Query.Where(b => b.Ticker == ticker && b.Date > exclusive)
             .OrderBy(b => b.Date)
             .Take(1);
    }
}
