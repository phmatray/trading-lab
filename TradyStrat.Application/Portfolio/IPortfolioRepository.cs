using TradyStrat.Domain.SeedWork;
using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;

namespace TradyStrat.Application.Portfolio;

public interface IPortfolioRepository
{
    Task<PortfolioAr> GetAsync(CancellationToken ct);

    /// <summary>
    /// Persists the AR's pending changes and returns the drained domain
    /// events for the caller to dispatch. After this call the AR's
    /// DomainEvents is empty.
    /// </summary>
    Task<IReadOnlyList<IDomainEvent>> SaveAsync(PortfolioAr portfolio, CancellationToken ct);
}
