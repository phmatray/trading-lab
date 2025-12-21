using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Outbound port for portfolio persistence operations.
/// Implemented by infrastructure layer (e.g., PortfolioRepository).
/// </summary>
public interface IPortfolioPort
{
    // Portfolio CRUD Operations

    /// <summary>
    /// Creates a new portfolio.
    /// </summary>
    /// <param name="name">Portfolio name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="initialCash">Initial cash balance.</param>
    /// <returns>The created portfolio with ID assigned.</returns>
    Task<Portfolio> CreatePortfolioAsync(string name, string? description, decimal initialCash);

    /// <summary>
    /// Gets a portfolio by ID with all positions loaded.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>The portfolio or null if not found.</returns>
    Task<Portfolio?> GetPortfolioByIdAsync(int portfolioId);

    /// <summary>
    /// Gets all portfolios with their positions.
    /// </summary>
    /// <returns>List of all portfolios.</returns>
    Task<List<Portfolio>> GetAllPortfoliosAsync();

    /// <summary>
    /// Updates an existing portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio to update.</param>
    Task UpdatePortfolioAsync(Portfolio portfolio);

    /// <summary>
    /// Deletes a portfolio and all its positions.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID to delete.</param>
    Task DeletePortfolioAsync(int portfolioId);

    // Cash Management Operations

    /// <summary>
    /// Adds cash to a portfolio (deposit).
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <param name="amount">Amount to add.</param>
    /// <param name="notes">Optional notes about the deposit.</param>
    Task AddCashAsync(int portfolioId, decimal amount, string? notes);

    /// <summary>
    /// Withdraws cash from a portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <param name="amount">Amount to withdraw.</param>
    /// <param name="notes">Optional notes about the withdrawal.</param>
    Task WithdrawCashAsync(int portfolioId, decimal amount, string? notes);

    /// <summary>
    /// Gets the cash transaction history for a portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>List of cash transactions.</returns>
    Task<List<PortfolioCashTransaction>> GetCashTransactionsAsync(int portfolioId);

    // Position Management Operations

    /// <summary>
    /// Adds a new position to a portfolio.
    /// </summary>
    /// <param name="position">The position to add.</param>
    /// <returns>The created position with ID assigned.</returns>
    Task<Position> AddPositionAsync(Position position);

    /// <summary>
    /// Updates an existing position.
    /// </summary>
    /// <param name="position">The position to update.</param>
    Task UpdatePositionAsync(Position position);

    /// <summary>
    /// Deletes a position.
    /// </summary>
    /// <param name="positionId">The position ID to delete.</param>
    Task DeletePositionAsync(int positionId);

    /// <summary>
    /// Gets all positions for a portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>List of positions.</returns>
    Task<List<Position>> GetPositionsByPortfolioAsync(int portfolioId);
}
