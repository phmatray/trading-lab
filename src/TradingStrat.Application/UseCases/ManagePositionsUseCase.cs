using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for managing portfolio positions (add, update, delete).
/// </summary>
public class ManagePositionsUseCase : IManagePositionsUseCase
{
    private readonly IPortfolioPort _portfolioPort;

    public ManagePositionsUseCase(IPortfolioPort portfolioPort)
    {
        _portfolioPort = portfolioPort ?? throw new ArgumentNullException(nameof(portfolioPort));
    }

    /// <inheritdoc />
    public async Task<Result<Position>> AddPositionAsync(AddPositionCommand command)
    {
        // Command validation happens in constructor - command is guaranteed to be valid here

        try
        {
            // Verify portfolio exists
            var portfolio = await _portfolioPort.GetPortfolioByIdAsync(command.PortfolioId);
            if (portfolio == null)
            {
                return Result<Position>.Failure(
                    Error.NotFound($"Portfolio {command.PortfolioId} not found", "PORTFOLIO_NOT_FOUND"));
            }

            // Check for duplicate position (same ticker in same portfolio)
            var existingPositions = await _portfolioPort.GetPositionsByPortfolioAsync(command.PortfolioId);
            if (existingPositions.Any(p => p.Ticker == command.Ticker))
            {
                return Result<Position>.Failure(
                    Error.Conflict($"Position for {command.Ticker} already exists in this portfolio", "POSITION_ALREADY_EXISTS"));
            }

            // Create position entity (Ticker is already normalized by command)
            var position = new Position
            {
                PortfolioId = command.PortfolioId,
                Ticker = command.Ticker,
                Quantity = command.Quantity,
                EntryPrice = command.EntryPrice,
                EntryDate = command.EntryDate,
                Notes = command.Notes
            };

            // Add via repository
            var createdPosition = await _portfolioPort.AddPositionAsync(position);
            return Result<Position>.Success(createdPosition);
        }
        catch (Exception ex)
        {
            return Result<Position>.Failure(
                Error.BusinessRule($"Failed to add position: {ex.Message}", "POSITION_ADD_FAILED"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Position>> UpdatePositionAsync(UpdatePositionCommand command)
    {
        // Command validation happens in constructor - command is guaranteed to be valid here

        try
        {
            // Load existing position to preserve immutable fields
            Position? existingPosition = await _portfolioPort.GetPositionByIdAsync(command.PositionId);
            if (existingPosition == null)
            {
                return Result<Position>.Failure(
                    Error.NotFound($"Position {command.PositionId} not found", "POSITION_NOT_FOUND"));
            }

            // Update only mutable fields
            existingPosition.Quantity = command.Quantity;
            existingPosition.EntryPrice = command.EntryPrice;
            existingPosition.Notes = command.Notes;

            await _portfolioPort.UpdatePositionAsync(existingPosition);
            return Result<Position>.Success(existingPosition);
        }
        catch (Exception ex)
        {
            return Result<Position>.Failure(
                Error.BusinessRule($"Failed to update position: {ex.Message}", "POSITION_UPDATE_FAILED"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeletePositionAsync(int positionId)
    {
        try
        {
            // Verify position exists before deleting
            var position = await _portfolioPort.GetPositionByIdAsync(positionId);
            if (position == null)
            {
                return Result<bool>.Failure(
                    Error.NotFound($"Position {positionId} not found", "POSITION_NOT_FOUND"));
            }

            await _portfolioPort.DeletePositionAsync(positionId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(
                Error.BusinessRule($"Failed to delete position: {ex.Message}", "POSITION_DELETE_FAILED"));
        }
    }
}
