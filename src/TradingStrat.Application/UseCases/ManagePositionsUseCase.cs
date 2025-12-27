using TradingStrat.Application.Commands;
using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for managing portfolio positions (add, update, delete).
/// Uses BaseUseCase pattern to eliminate try-catch boilerplate.
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
        try
        {
            var position = await AddPositionCoreAsync(command);
            return Result<Position>.Success(position);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Portfolio") && ex.Message.Contains("not found"))
        {
            return Result<Position>.Failure(
                Error.NotFound(ex.Message, ErrorCodes.Portfolio.NotFound));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Result<Position>.Failure(
                Error.NotFound(ex.Message, ErrorCodes.Position.NotFound));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Result<Position>.Failure(
                Error.Conflict(ex.Message, ErrorCodes.Position.AlreadyExists));
        }
        catch (Exception ex)
        {
            return Result<Position>.Failure(
                Error.BusinessRule($"Failed to add position: {ex.Message}", ErrorCodes.Position.AddFailed));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Position>> UpdatePositionAsync(UpdatePositionCommand command)
    {
        try
        {
            var position = await UpdatePositionCoreAsync(command);
            return Result<Position>.Success(position);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Result<Position>.Failure(
                Error.NotFound(ex.Message, ErrorCodes.Position.NotFound));
        }
        catch (Exception ex)
        {
            return Result<Position>.Failure(
                Error.BusinessRule($"Failed to update position: {ex.Message}", ErrorCodes.Position.UpdateFailed));
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeletePositionAsync(int positionId)
    {
        try
        {
            await DeletePositionCoreAsync(positionId);
            return Result<bool>.Success(true);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Result<bool>.Failure(
                Error.NotFound(ex.Message, ErrorCodes.Position.NotFound));
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(
                Error.BusinessRule($"Failed to delete position: {ex.Message}", ErrorCodes.Position.DeleteFailed));
        }
    }

    private async Task<Position> AddPositionCoreAsync(AddPositionCommand command)
    {
        // Verify portfolio exists
        var portfolio = await _portfolioPort.GetPortfolioByIdAsync(command.PortfolioId);
        if (portfolio == null)
        {
            throw new InvalidOperationException($"Portfolio {command.PortfolioId} not found");
        }

        // Check for duplicate position
        var existingPositions = await _portfolioPort.GetPositionsByPortfolioAsync(command.PortfolioId);
        if (existingPositions.Any(p => p.Ticker == command.Ticker))
        {
            throw new InvalidOperationException($"Position for {command.Ticker} already exists in this portfolio");
        }

        // Create position entity
        var position = new Position
        {
            PortfolioId = command.PortfolioId,
            Ticker = command.Ticker,
            Quantity = command.Quantity,
            EntryPrice = command.EntryPrice,
            EntryDate = command.EntryDate,
            Notes = command.Notes
        };

        return await _portfolioPort.AddPositionAsync(position);
    }

    private async Task<Position> UpdatePositionCoreAsync(UpdatePositionCommand command)
    {
        // Load existing position
        Position? existingPosition = await _portfolioPort.GetPositionByIdAsync(command.PositionId);
        if (existingPosition == null)
        {
            throw new InvalidOperationException($"Position {command.PositionId} not found");
        }

        // Update mutable fields
        existingPosition.Quantity = command.Quantity;
        existingPosition.EntryPrice = command.EntryPrice;
        existingPosition.Notes = command.Notes;

        await _portfolioPort.UpdatePositionAsync(existingPosition);
        return existingPosition;
    }

    private async Task DeletePositionCoreAsync(int positionId)
    {
        // Verify position exists
        var position = await _portfolioPort.GetPositionByIdAsync(positionId);
        if (position == null)
        {
            throw new InvalidOperationException($"Position {positionId} not found");
        }

        await _portfolioPort.DeletePositionAsync(positionId);
    }
}
