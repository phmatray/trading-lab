using Microsoft.EntityFrameworkCore;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Infrastructure.Persistence.EfCore;

/// <summary>
/// Repository implementation for portfolio persistence using Entity Framework Core.
/// </summary>
public class PortfolioRepository : IPortfolioPort
{
    private readonly TradingContext _context;

    public PortfolioRepository(TradingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Portfolio CRUD Operations

    /// <inheritdoc />
    public async Task<Portfolio> CreatePortfolioAsync(string name, string? description, decimal initialCash)
    {
        var portfolio = new Portfolio
        {
            Name = name,
            Description = description,
            Cash = initialCash,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        _context.Portfolios.Add(portfolio);
        await _context.SaveChangesAsync();

        return portfolio;
    }

    /// <inheritdoc />
    public async Task<Portfolio?> GetPortfolioByIdAsync(int portfolioId)
    {
        return await _context.Portfolios
            .Include(p => p.Positions)
            .FirstOrDefaultAsync(p => p.Id == portfolioId);
    }

    /// <inheritdoc />
    public async Task<List<Portfolio>> GetAllPortfoliosAsync()
    {
        return await _context.Portfolios
            .Include(p => p.Positions)
            .OrderByDescending(p => p.LastUpdated)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task UpdatePortfolioAsync(Portfolio portfolio)
    {
        portfolio.LastUpdated = DateTime.UtcNow;
        _context.Portfolios.Update(portfolio);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeletePortfolioAsync(int portfolioId)
    {
        Portfolio? portfolio = await _context.Portfolios.FindAsync(portfolioId);
        if (portfolio is not null)
        {
            _context.Portfolios.Remove(portfolio);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Cash Management Operations

    /// <inheritdoc />
    public async Task AddCashAsync(int portfolioId, decimal amount, string? notes)
    {
        Portfolio? portfolio = await GetPortfolioByIdAsync(portfolioId);
        if (portfolio is null)
        {
            throw new InvalidOperationException($"Portfolio {portfolioId} not found");
        }

        portfolio.Cash += amount;

        _context.CashTransactions.Add(new PortfolioCashTransaction
        {
            PortfolioId = portfolioId,
            Type = TransactionType.Deposit,
            Amount = amount,
            TransactionDate = DateTime.UtcNow,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        });

        await UpdatePortfolioAsync(portfolio);
    }

    /// <inheritdoc />
    public async Task WithdrawCashAsync(int portfolioId, decimal amount, string? notes)
    {
        Portfolio? portfolio = await GetPortfolioByIdAsync(portfolioId);
        if (portfolio is null)
        {
            throw new InvalidOperationException($"Portfolio {portfolioId} not found");
        }

        if (portfolio.Cash < amount)
        {
            throw new InvalidOperationException(
                $"Insufficient cash balance. Available: {portfolio.Cash:C2}, Requested: {amount:C2}");
        }

        portfolio.Cash -= amount;

        _context.CashTransactions.Add(new PortfolioCashTransaction
        {
            PortfolioId = portfolioId,
            Type = TransactionType.Withdrawal,
            Amount = amount,
            TransactionDate = DateTime.UtcNow,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        });

        await UpdatePortfolioAsync(portfolio);
    }

    /// <inheritdoc />
    public async Task<List<PortfolioCashTransaction>> GetCashTransactionsAsync(int portfolioId)
    {
        return await _context.CashTransactions
            .Where(t => t.PortfolioId == portfolioId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    #endregion

    #region Position Management Operations

    /// <inheritdoc />
    public async Task<Position> AddPositionAsync(Position position)
    {
        position.CreatedAt = DateTime.UtcNow;
        position.LastUpdated = DateTime.UtcNow;

        _context.Positions.Add(position);
        await _context.SaveChangesAsync();

        return position;
    }

    /// <inheritdoc />
    public async Task UpdatePositionAsync(Position position)
    {
        // Load the existing position from database
        Position? existingPosition = await _context.Positions.FindAsync(position.Id);
        if (existingPosition is null)
        {
            throw new InvalidOperationException($"Position {position.Id} not found");
        }

        // Update only the fields that should be modified
        // Preserve Ticker, PortfolioId, EntryDate (immutable after creation)
        existingPosition.Quantity = position.Quantity;
        existingPosition.EntryPrice = position.EntryPrice;
        existingPosition.Notes = position.Notes;
        existingPosition.LastUpdated = DateTime.UtcNow;

        _context.Positions.Update(existingPosition);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<Position?> GetPositionByIdAsync(int positionId)
    {
        return await _context.Positions.FindAsync(positionId);
    }

    /// <inheritdoc />
    public async Task DeletePositionAsync(int positionId)
    {
        Position? position = await _context.Positions.FindAsync(positionId);
        if (position is not null)
        {
            _context.Positions.Remove(position);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<List<Position>> GetPositionsByPortfolioAsync(int portfolioId)
    {
        return await _context.Positions
            .Where(p => p.PortfolioId == portfolioId)
            .OrderBy(p => p.Ticker)
            .ToListAsync();
    }

    #endregion
}
