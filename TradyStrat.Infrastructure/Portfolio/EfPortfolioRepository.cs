using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.Portfolio;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Data;
using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;

namespace TradyStrat.Infrastructure.Portfolio;

public sealed class EfPortfolioRepository(AppDbContext db) : IPortfolioRepository
{
    public async Task<PortfolioAr> GetAsync(CancellationToken ct)
    {
        var portfolio = await db.Portfolios
            .Include("_positions._openLots")
            .Include("_positions._trades")
            .SingleOrDefaultAsync(p => p.Id == PortfolioId.Singleton, ct);

        if (portfolio is null)
        {
            portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
            db.Portfolios.Add(portfolio);
            await db.SaveChangesAsync(ct);
            return portfolio;
        }

        // First-load rehydration: for each Position with trades but no open
        // lots (post-migration state), replay trades to derive lots + realized.
        if (portfolio.RehydrateLots())
            await db.SaveChangesAsync(ct);

        return portfolio;
    }

    public async Task SaveAsync(PortfolioAr portfolio, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }
}
