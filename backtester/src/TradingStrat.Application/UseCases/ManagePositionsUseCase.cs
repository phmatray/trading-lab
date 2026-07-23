using TradingStrat.Application.Commands;
using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for managing portfolio positions (add, update, delete).
/// Uses helper method pattern to eliminate try-catch boilerplate.
/// </summary>
public class ManagePositionsUseCase : IManagePositionsUseCase
{
    private readonly IPortfolioPort _portfolioPort;

    public ManagePositionsUseCase(IPortfolioPort portfolioPort)
    {
        _portfolioPort = portfolioPort ?? throw new ArgumentNullException(nameof(portfolioPort));
    }

    /// <inheritdoc />
    public Task<Result<Position>> AddPositionAsync(AddPositionCommand command)
        => ExecuteWithErrorHandling(() => AddPositionCoreAsync(command), ErrorCodes.Position.AddFailed);

    /// <inheritdoc />
    public Task<Result<Position>> UpdatePositionAsync(UpdatePositionCommand command)
        => ExecuteWithErrorHandling(() => UpdatePositionCoreAsync(command), ErrorCodes.Position.UpdateFailed);

    /// <inheritdoc />
    public Task<Result<bool>> DeletePositionAsync(int positionId)
        => ExecuteWithErrorHandling(() => DeletePositionCoreAsync(positionId), ErrorCodes.Position.DeleteFailed);

    private static async Task<Result<T>> ExecuteWithErrorHandling<T>(
        Func<Task<T>> executeCore,
        string errorCode)
    {
        try
        {
            T result = await executeCore();
            return Result<T>.Success(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Portfolio") && ex.Message.Contains("not found"))
        {
            return Result<T>.Failure(
                Error.NotFound(ex.Message, ErrorCodes.Portfolio.NotFound));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Position") && ex.Message.Contains("not found"))
        {
            return Result<T>.Failure(
                Error.NotFound(ex.Message, ErrorCodes.Position.NotFound));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Result<T>.Failure(
                Error.Conflict(ex.Message, ErrorCodes.Position.AlreadyExists));
        }
        catch (ArgumentException ex)
        {
            return Result<T>.Failure(
                Error.Validation(ex.Message, $"{errorCode}_VALIDATION"));
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(
                Error.BusinessRule($"Failed: {ex.Message}", $"{errorCode}_FAILED"));
        }
    }

    private async Task<Position> AddPositionCoreAsync(AddPositionCommand command)
    {
        // Verify portfolio exists
        Portfolio? portfolio = await _portfolioPort.GetPortfolioByIdAsync(command.PortfolioId);
        if (portfolio is null)
        {
            throw new InvalidOperationException($"Portfolio {command.PortfolioId} not found");
        }

        // Check for duplicate position
        List<Position> existingPositions = await _portfolioPort.GetPositionsByPortfolioAsync(command.PortfolioId);
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
        if (existingPosition is null)
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

    private async Task<bool> DeletePositionCoreAsync(int positionId)
    {
        // Verify position exists
        Position? position = await _portfolioPort.GetPositionByIdAsync(positionId);
        if (position is null)
        {
            throw new InvalidOperationException($"Position {positionId} not found");
        }

        await _portfolioPort.DeletePositionAsync(positionId);
        return true;
    }
}
