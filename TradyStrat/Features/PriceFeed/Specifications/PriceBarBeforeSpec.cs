using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Features.PriceFeed.Specifications;

public sealed class PriceBarBeforeSpec : Specification<PriceBar>
{
    public PriceBarBeforeSpec(string ticker, DateOnly exclusive)
    {
        Query.Where(b => b.Ticker == ticker && b.Date < exclusive)
             .OrderByDescending(b => b.Date)
             .Take(1);
    }
}
