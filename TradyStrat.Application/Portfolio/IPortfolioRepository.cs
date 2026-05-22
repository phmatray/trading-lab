using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;

namespace TradyStrat.Application.Portfolio;

public interface IPortfolioRepository
{
    Task<PortfolioAr> GetAsync(CancellationToken ct);
    Task SaveAsync(PortfolioAr portfolio, CancellationToken ct);
}
