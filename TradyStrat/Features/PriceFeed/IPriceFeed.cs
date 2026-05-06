using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.PriceFeed;

public interface IPriceFeed
{
    Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
        string ticker, DateOnly from, DateOnly to, CancellationToken ct);
}
