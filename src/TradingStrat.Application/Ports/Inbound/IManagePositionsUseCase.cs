using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Command to add a new position to a portfolio.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record AddPositionCommand
{
    public int PortfolioId { get; init; }
    public string Ticker { get; init; }
    public int Quantity { get; init; }
    public decimal EntryPrice { get; init; }
    public DateTime EntryDate { get; init; }
    public string? Notes { get; init; }

    public AddPositionCommand(
        int PortfolioId,
        string Ticker,
        int Quantity,
        decimal EntryPrice,
        DateTime EntryDate,
        string? Notes = null)
    {
        // Validate parameters
        ValidationGuard.Require(PortfolioId).GreaterThan(0, "Portfolio ID must be positive");
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();
        ValidationGuard.Require(Quantity).GreaterThan(0, "Quantity must be positive");
        ValidationGuard.Require(EntryPrice).GreaterThan(0m, "Entry price must be positive");
        ValidationGuard.Require(EntryDate).LessThanOrEqual(DateTime.Today, "Entry date cannot be in the future");

        // Assign validated values
        this.PortfolioId = PortfolioId;
        this.Ticker = Ticker.ToUpperInvariant().Trim();
        this.Quantity = Quantity;
        this.EntryPrice = EntryPrice;
        this.EntryDate = EntryDate;
        this.Notes = Notes;
    }
}

/// <summary>
/// Command to update an existing position.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record UpdatePositionCommand
{
    public int PositionId { get; init; }
    public int Quantity { get; init; }
    public decimal EntryPrice { get; init; }
    public string? Notes { get; init; }

    public UpdatePositionCommand(
        int PositionId,
        int Quantity,
        decimal EntryPrice,
        string? Notes = null)
    {
        // Validate parameters
        ValidationGuard.Require(PositionId).GreaterThan(0, "Position ID must be positive");
        ValidationGuard.Require(Quantity).GreaterThan(0, "Quantity must be positive");
        ValidationGuard.Require(EntryPrice).GreaterThan(0m, "Entry price must be positive");

        // Assign validated values
        this.PositionId = PositionId;
        this.Quantity = Quantity;
        this.EntryPrice = EntryPrice;
        this.Notes = Notes;
    }
}

/// <summary>
/// Inbound port (use case interface) for managing positions.
/// </summary>
public interface IManagePositionsUseCase
{
    /// <summary>
    /// Adds a new position to a portfolio.
    /// </summary>
    /// <param name="command">The add position command.</param>
    /// <returns>Result containing the created position, or errors if the operation failed.</returns>
    Task<Result<Position>> AddPositionAsync(AddPositionCommand command);

    /// <summary>
    /// Updates an existing position.
    /// </summary>
    /// <param name="command">The update position command.</param>
    /// <returns>Result containing the updated position, or errors if the operation failed.</returns>
    Task<Result<Position>> UpdatePositionAsync(UpdatePositionCommand command);

    /// <summary>
    /// Deletes a position.
    /// </summary>
    /// <param name="positionId">The position ID to delete.</param>
    /// <returns>Result indicating success or failure with errors.</returns>
    Task<Result<bool>> DeletePositionAsync(int positionId);
}
