using TradyStrat.Application.PriceFeed;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.PriceFeed;

internal sealed class EfPriceFeedWriter(AppDbContext db) : IPriceFeedWriter
{
    public async Task AppendAsync(IReadOnlyList<PriceBar> bars, CancellationToken ct)
    {
        db.PriceBars.AddRange(bars);
        await db.SaveChangesAsync(ct);
    }
}
