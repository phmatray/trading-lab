using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.Portfolio;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Data;
using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;

namespace TradyStrat.Infrastructure.Portfolio;

public sealed class EfPortfolioRepository(
    AppDbContext db,
    IClock clock,
    IDomainEventDispatcher dispatcher) : IPortfolioRepository
{
    public async Task<PortfolioAr> GetAsync(CancellationToken ct)
    {
        var portfolio = await db.Portfolios
            .Include("_positions._openLots")
            .Include("_positions._trades")
            .SingleOrDefaultAsync(p => p.Id == PortfolioId.Singleton, ct);

        if (portfolio is null)
        {
            // First-ever read: bootstrap the singleton AR. Empty(id, now) raises
            // PortfolioCreated; we persist, then dispatch through the standard
            // pipeline so any future handler observes the first-install event.
            portfolio = PortfolioAr.Empty(PortfolioId.Singleton, clock.UtcNow());
            db.Portfolios.Add(portfolio);
            await db.SaveChangesAsync(ct);
            await dispatcher.DispatchAsync(portfolio.DequeueDomainEvents(), ct);
            return portfolio;
        }

        // First-load rehydration: for each Position with trades but no open
        // lots (post-migration state), replay trades to derive lots + realized.
        if (portfolio.RehydrateLots())
            await db.SaveChangesAsync(ct);

        return portfolio;
    }

    public async Task<IReadOnlyList<IDomainEvent>> SaveAsync(PortfolioAr portfolio, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        return portfolio.DequeueDomainEvents();
    }
}
