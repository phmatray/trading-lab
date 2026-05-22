using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.Fx;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Infrastructure.Fx.Specifications;

namespace TradyStrat.Infrastructure.Fx;

public sealed class EfFxRateReadRepository(AppDbContext db) : IFxRateReadRepository
{
    public Task<FxRate?> LatestAsync(string @base, string quote, DateOnly asOf, CancellationToken ct)
        => db.FxRates.WithSpecification(new LatestFxRateSpec(@base, quote, asOf)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<FxRate>> ListForPairAsync(string @base, string quote, CancellationToken ct)
    {
        var b = Currency.Parse(@base);
        var q = Currency.Parse(quote);
        return await db.FxRates
            .Where(r => r.Pair.Base == b && r.Pair.Quote == q)
            .OrderByDescending(r => r.Date)
            .ToListAsync(ct);
    }
}
