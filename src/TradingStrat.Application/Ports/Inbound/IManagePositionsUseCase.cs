using TradingStrat.Application.Commands;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Inbound;

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
