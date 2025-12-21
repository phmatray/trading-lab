namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Command to create a new portfolio.
/// </summary>
/// <param name="Name">Portfolio name (required).</param>
/// <param name="Description">Optional portfolio description.</param>
/// <param name="InitialCash">Initial cash balance.</param>
public record CreatePortfolioCommand(
    string Name,
    string? Description,
    decimal InitialCash
);

/// <summary>
/// Result of creating a portfolio.
/// </summary>
/// <param name="PortfolioId">The ID of the created portfolio.</param>
/// <param name="Name">The portfolio name.</param>
/// <param name="InitialCash">The initial cash balance.</param>
/// <param name="CreatedAt">When the portfolio was created.</param>
public record CreatePortfolioResult(
    int PortfolioId,
    string Name,
    decimal InitialCash,
    DateTime CreatedAt
);

/// <summary>
/// Inbound port (use case interface) for creating portfolios.
/// </summary>
public interface ICreatePortfolioUseCase
{
    /// <summary>
    /// Creates a new portfolio.
    /// </summary>
    /// <param name="command">The create portfolio command.</param>
    /// <returns>The created portfolio result.</returns>
    Task<CreatePortfolioResult> ExecuteAsync(CreatePortfolioCommand command);
}
