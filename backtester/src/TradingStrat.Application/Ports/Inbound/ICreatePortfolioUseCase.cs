using TradingStrat.Application.Commands;
using TradingStrat.Domain.Common;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Inbound port (use case interface) for creating portfolios.
/// </summary>
public interface ICreatePortfolioUseCase
{
    /// <summary>
    /// Creates a new portfolio.
    /// </summary>
    /// <param name="command">The create portfolio command.</param>
    /// <returns>Result containing the created portfolio information, or errors if creation failed.</returns>
    Task<Result<CreatePortfolioResult>> ExecuteAsync(CreatePortfolioCommand command);
}
