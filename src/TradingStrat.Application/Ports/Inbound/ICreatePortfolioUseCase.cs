using TradingStrat.Domain.Common;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Command to create a new portfolio.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record CreatePortfolioCommand
{
    public string Name { get; init; }
    public string? Description { get; init; }
    public decimal InitialCash { get; init; }

    public CreatePortfolioCommand(
        string Name,
        string? Description = null,
        decimal InitialCash = 0m)
    {
        // Validate parameters
        ValidationGuard.Require(Name).NotNullOrWhiteSpace();
        ValidationGuard.Require(InitialCash).GreaterThanOrEqual(0m, "Initial cash cannot be negative");

        // Assign validated values
        this.Name = Name.Trim();
        this.Description = Description?.Trim();
        this.InitialCash = InitialCash;
    }
}

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
