using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
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
    public async Task<Position> AddPositionAsync(AddPositionCommand command)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(command.Ticker))
        {
            throw new ArgumentException("Ticker is required", nameof(command));
        }

        if (command.Quantity <= 0)
        {
            throw new ArgumentException("Quantity must be positive", nameof(command));
        }

        if (command.EntryPrice <= 0)
        {
            throw new ArgumentException("Entry price must be positive", nameof(command));
        }

        // Verify portfolio exists
        var portfolio = await _portfolioPort.GetPortfolioByIdAsync(command.PortfolioId);
        if (portfolio == null)
        {
            throw new InvalidOperationException($"Portfolio {command.PortfolioId} not found");
        }

        // Create position entity
        var position = new Position
        {
            PortfolioId = command.PortfolioId,
            Ticker = command.Ticker.ToUpperInvariant(),
            Quantity = command.Quantity,
            EntryPrice = command.EntryPrice,
            EntryDate = command.EntryDate,
            Notes = command.Notes
        };

        // Add via repository
        return await _portfolioPort.AddPositionAsync(position);
    }

    /// <inheritdoc />
    public async Task<Position> UpdatePositionAsync(UpdatePositionCommand command)
    {
        // Validate input
        if (command.Quantity <= 0)
        {
            throw new ArgumentException("Quantity must be positive", nameof(command));
        }

        if (command.EntryPrice <= 0)
        {
            throw new ArgumentException("Entry price must be positive", nameof(command));
        }

        // Load existing position to preserve immutable fields
        Position? existingPosition = await _portfolioPort.GetPositionByIdAsync(command.PositionId);
        if (existingPosition == null)
        {
            throw new InvalidOperationException($"Position {command.PositionId} not found");
        }

        // Update only mutable fields
        existingPosition.Quantity = command.Quantity;
        existingPosition.EntryPrice = command.EntryPrice;
        existingPosition.Notes = command.Notes;

        await _portfolioPort.UpdatePositionAsync(existingPosition);
        return existingPosition;
    }

    /// <inheritdoc />
    public async Task DeletePositionAsync(int positionId)
    {
        await _portfolioPort.DeletePositionAsync(positionId);
    }
}
