using Ardalis.Specification;
using TradyStrat.Shared.Domain;
using TradyStrat.Specifications.PriceBars;
using TradyStrat.Specifications.Trades;

namespace TradyStrat.Features.Portfolio;

public sealed class GrowthSeriesBuilder(
    IReadRepositoryBase<Trade> trades,
    IReadRepositoryBase<PriceBar> bars)
{
    public async Task<IReadOnlyList<GrowthPoint>> BuildAsync(string ticker, CancellationToken ct)
    {
        var allTrades = await trades.ListAsync(new AllTradesSpec(), ct);
        if (allTrades.Count == 0) return [];

        var firstDate = allTrades[0].ExecutedOn;
        var priceBars = await bars.ListAsync(new PriceBarsSinceSpec(ticker, firstDate), ct);
        if (priceBars.Count == 0) return [];

        var tradeByDate = allTrades
            .GroupBy(t => t.ExecutedOn)
            .ToDictionary(g => g.Key, g => g.ToList());

        var points = new List<GrowthPoint>(priceBars.Count);
        var shares = 0m;

        foreach (var bar in priceBars)
        {
            if (tradeByDate.TryGetValue(bar.Date, out var todaysTrades))
            {
                foreach (var t in todaysTrades)
                    shares += t.IsBuy ? t.Quantity : -t.Quantity;
            }
            points.Add(new GrowthPoint(bar.Date, shares * bar.Close));
        }

        return points;
    }
}
