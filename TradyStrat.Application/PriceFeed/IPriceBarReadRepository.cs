using TradyStrat.Domain;

namespace TradyStrat.Application.PriceFeed;

public interface IPriceBarReadRepository
{
    Task<IReadOnlyList<PriceBar>> ListForTickerAsync(string ticker, CancellationToken ct);
    Task<IReadOnlyList<PriceBar>> ListAsOfAsync(string ticker, DateOnly asOf, CancellationToken ct);
    Task<IReadOnlyList<PriceBar>> ListSinceAsync(string ticker, DateOnly since, CancellationToken ct);
    Task<IReadOnlyList<PriceBar>> ListInRangeAsync(string ticker, DateOnly from, DateOnly to, CancellationToken ct);
    Task<PriceBar?>               LatestAsync(string ticker, CancellationToken ct);
    Task<PriceBar?>               EarliestAsync(string ticker, CancellationToken ct);
    Task<PriceBar?>               LatestBeforeAsync(string ticker, DateOnly date, CancellationToken ct);
    Task<PriceBar?>               EarliestAfterAsync(string ticker, DateOnly date, CancellationToken ct);
}
