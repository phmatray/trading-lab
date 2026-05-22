using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.PriceFeed;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Infrastructure.PriceFeed.Specifications;

namespace TradyStrat.Infrastructure.PriceFeed;

public sealed class EfPriceBarReadRepository(AppDbContext db) : IPriceBarReadRepository
{
    public async Task<IReadOnlyList<PriceBar>> ListForTickerAsync(string ticker, CancellationToken ct)
        => await db.PriceBars.WithSpecification(new PriceBarsForTickerSpec(ticker)).ToListAsync(ct);

    public async Task<IReadOnlyList<PriceBar>> ListAsOfAsync(string ticker, DateOnly asOf, CancellationToken ct)
        => await db.PriceBars.WithSpecification(new PriceBarsAsOfSpec(ticker, asOf)).ToListAsync(ct);

    public async Task<IReadOnlyList<PriceBar>> ListSinceAsync(string ticker, DateOnly since, CancellationToken ct)
        => await db.PriceBars.WithSpecification(new PriceBarsSinceSpec(ticker, since)).ToListAsync(ct);

    public async Task<IReadOnlyList<PriceBar>> ListInRangeAsync(string ticker, DateOnly from, DateOnly to, CancellationToken ct)
        => await db.PriceBars.WithSpecification(new PriceBarsInRangeSpec(ticker, from, to)).ToListAsync(ct);

    public Task<PriceBar?> LatestAsync(string ticker, CancellationToken ct)
        => db.PriceBars.WithSpecification(new LatestPriceBarSpec(ticker)).FirstOrDefaultAsync(ct);

    public Task<PriceBar?> EarliestAsync(string ticker, CancellationToken ct)
        => db.PriceBars.WithSpecification(new EarliestPriceBarSpec(ticker)).FirstOrDefaultAsync(ct);

    public Task<PriceBar?> LatestBeforeAsync(string ticker, DateOnly date, CancellationToken ct)
        => db.PriceBars.WithSpecification(new PriceBarBeforeSpec(ticker, date)).FirstOrDefaultAsync(ct);

    public Task<PriceBar?> EarliestAfterAsync(string ticker, DateOnly date, CancellationToken ct)
        => db.PriceBars.WithSpecification(new PriceBarAfterSpec(ticker, date)).FirstOrDefaultAsync(ct);
}
