using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Command to add a new position to a portfolio.
/// </summary>
/// <param name="PortfolioId">The portfolio ID.</param>
/// <param name="Ticker">The ticker symbol.</param>
/// <param name="Quantity">Number of shares.</param>
/// <param name="EntryPrice">Entry price per share.</param>
/// <param name="EntryDate">Date the position was entered.</param>
/// <param name="Notes">Optional notes.</param>
public record AddPositionCommand(
    int PortfolioId,
    string Ticker,
    int Quantity,
    decimal EntryPrice,
    DateTime EntryDate,
    string? Notes
);

/// <summary>
/// Command to update an existing position.
/// </summary>
/// <param name="PositionId">The position ID.</param>
/// <param name="Quantity">Updated quantity.</param>
/// <param name="EntryPrice">Updated entry price.</param>
/// <param name="Notes">Updated notes.</param>
public record UpdatePositionCommand(
    int PositionId,
    int Quantity,
    decimal EntryPrice,
    string? Notes
);

/// <summary>
/// Inbound port (use case interface) for managing positions.
/// </summary>
public interface IManagePositionsUseCase
{
    /// <summary>
    /// Adds a new position to a portfolio.
    /// </summary>
    /// <param name="command">The add position command.</param>
    /// <returns>The created position.</returns>
    Task<Position> AddPositionAsync(AddPositionCommand command);

    /// <summary>
    /// Updates an existing position.
    /// </summary>
    /// <param name="command">The update position command.</param>
    /// <returns>The updated position.</returns>
    Task<Position> UpdatePositionAsync(UpdatePositionCommand command);

    /// <summary>
    /// Deletes a position.
    /// </summary>
    /// <param name="positionId">The position ID to delete.</param>
    Task DeletePositionAsync(int positionId);
}
