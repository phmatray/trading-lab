using System.Reflection;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Tests.TestDoubles;

/// <summary>
/// In-memory implementation of IPortfolioPort for testing.
/// Perfect for unit tests - no actual database needed.
/// </summary>
public class InMemoryPortfolioRepository : IPortfolioPort
{
    private readonly Dictionary<int, Portfolio> _portfolios = new();
    private readonly Dictionary<int, Position> _positions = new();
    private readonly List<PortfolioCashTransaction> _transactions = new();
    private int _nextPortfolioId = 1;
    private int _nextPositionId = 1;
    private int _nextTransactionId = 1;

    #region Portfolio CRUD Operations

    public Task<Portfolio> CreatePortfolioAsync(string name, string? description, decimal initialCash)
    {
        Portfolio portfolio = new()
        {
            Id = _nextPortfolioId++,
            Name = name,
            Description = description,
            Cash = initialCash,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        _portfolios[portfolio.Id] = portfolio;
        return Task.FromResult(portfolio);
    }

    public Task<Portfolio?> GetPortfolioByIdAsync(int portfolioId)
    {
        if (!_portfolios.TryGetValue(portfolioId, out Portfolio? portfolio))
        {
            return Task.FromResult<Portfolio?>(null);
        }

        // Load positions for this portfolio using reflection (test infrastructure only)
        List<Position> positions = _positions.Values
            .Where(p => p.PortfolioId == portfolioId)
            .ToList();
        LoadPositionsIntoPortfolio(portfolio, positions);

        return Task.FromResult<Portfolio?>(portfolio);
    }

    public Task<List<Portfolio>> GetAllPortfoliosAsync()
    {
        List<Portfolio> portfolios = _portfolios.Values
            .OrderByDescending(p => p.LastUpdated)
            .ToList();

        // Load positions for each portfolio using reflection (test infrastructure only)
        foreach (Portfolio portfolio in portfolios)
        {
            List<Position> positions = _positions.Values
                .Where(p => p.PortfolioId == portfolio.Id)
                .ToList();
            LoadPositionsIntoPortfolio(portfolio, positions);
        }

        return Task.FromResult(portfolios);
    }

    public Task UpdatePortfolioAsync(Portfolio portfolio)
    {
        if (!_portfolios.ContainsKey(portfolio.Id))
        {
            throw new InvalidOperationException($"Portfolio {portfolio.Id} not found");
        }

        portfolio.LastUpdated = DateTime.UtcNow;
        _portfolios[portfolio.Id] = portfolio;
        return Task.CompletedTask;
    }

    public Task DeletePortfolioAsync(int portfolioId)
    {
        if (_portfolios.ContainsKey(portfolioId))
        {
            _portfolios.Remove(portfolioId);

            // Delete all positions for this portfolio (cascade delete)
            List<int> positionIdsToRemove = _positions.Values
                .Where(p => p.PortfolioId == portfolioId)
                .Select(p => p.Id)
                .ToList();

            foreach (int positionId in positionIdsToRemove)
            {
                _positions.Remove(positionId);
            }

            // Delete all cash transactions for this portfolio
            _transactions.RemoveAll(t => t.PortfolioId == portfolioId);
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Cash Management Operations

    public async Task AddCashAsync(int portfolioId, decimal amount, string? notes)
    {
        Portfolio? portfolio = await GetPortfolioByIdAsync(portfolioId);
        if (portfolio == null)
        {
            throw new InvalidOperationException($"Portfolio {portfolioId} not found");
        }

        portfolio.Cash += amount;

        _transactions.Add(new PortfolioCashTransaction
        {
            Id = _nextTransactionId++,
            PortfolioId = portfolioId,
            Type = TransactionType.Deposit,
            Amount = amount,
            TransactionDate = DateTime.UtcNow,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        });

        await UpdatePortfolioAsync(portfolio);
    }

    public async Task WithdrawCashAsync(int portfolioId, decimal amount, string? notes)
    {
        Portfolio? portfolio = await GetPortfolioByIdAsync(portfolioId);
        if (portfolio == null)
        {
            throw new InvalidOperationException($"Portfolio {portfolioId} not found");
        }

        if (portfolio.Cash < amount)
        {
            throw new InvalidOperationException(
                $"Insufficient cash balance. Available: {portfolio.Cash:C2}, Requested: {amount:C2}");
        }

        portfolio.Cash -= amount;

        _transactions.Add(new PortfolioCashTransaction
        {
            Id = _nextTransactionId++,
            PortfolioId = portfolioId,
            Type = TransactionType.Withdrawal,
            Amount = amount,
            TransactionDate = DateTime.UtcNow,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        });

        await UpdatePortfolioAsync(portfolio);
    }

    public Task<List<PortfolioCashTransaction>> GetCashTransactionsAsync(int portfolioId)
    {
        List<PortfolioCashTransaction> transactions = _transactions
            .Where(t => t.PortfolioId == portfolioId)
            .OrderByDescending(t => t.TransactionDate)
            .ToList();

        return Task.FromResult(transactions);
    }

    #endregion

    #region Position Management Operations

    public Task<Position> AddPositionAsync(Position position)
    {
        // Check for duplicate ticker in same portfolio
        bool duplicateExists = _positions.Values.Any(p =>
            p.PortfolioId == position.PortfolioId &&
            p.Ticker == position.Ticker);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                $"Position with ticker {position.Ticker} already exists in portfolio {position.PortfolioId}");
        }

        position.Id = _nextPositionId++;
        position.CreatedAt = DateTime.UtcNow;
        position.LastUpdated = DateTime.UtcNow;

        _positions[position.Id] = position;
        return Task.FromResult(position);
    }

    public Task UpdatePositionAsync(Position position)
    {
        if (!_positions.ContainsKey(position.Id))
        {
            throw new InvalidOperationException($"Position {position.Id} not found");
        }

        // Preserve immutable fields (Ticker, PortfolioId, EntryDate)
        Position existingPosition = _positions[position.Id];
        existingPosition.Quantity = position.Quantity;
        existingPosition.EntryPrice = position.EntryPrice;
        existingPosition.Notes = position.Notes;
        existingPosition.LastUpdated = DateTime.UtcNow;

        return Task.CompletedTask;
    }

    public Task<Position?> GetPositionByIdAsync(int positionId)
    {
        return Task.FromResult(_positions.TryGetValue(positionId, out Position? position)
            ? position
            : null);
    }

    public Task DeletePositionAsync(int positionId)
    {
        if (_positions.ContainsKey(positionId))
        {
            _positions.Remove(positionId);
        }

        return Task.CompletedTask;
    }

    public Task<List<Position>> GetPositionsByPortfolioAsync(int portfolioId)
    {
        List<Position> positions = _positions.Values
            .Where(p => p.PortfolioId == portfolioId)
            .OrderBy(p => p.Ticker)
            .ToList();

        return Task.FromResult(positions);
    }

    #endregion

    #region Test Helper Methods

    /// <summary>
    /// Loads positions into a portfolio using reflection.
    /// This is needed because Portfolio.Positions is read-only to enforce encapsulation.
    /// In production, EF Core's Include() handles this. For testing, we use reflection.
    /// </summary>
    private static void LoadPositionsIntoPortfolio(Portfolio portfolio, List<Position> positions)
    {
        FieldInfo? positionsField = typeof(Portfolio).GetField("_positions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (positionsField != null)
        {
            var positionsList = (List<Position>)positionsField.GetValue(portfolio)!;
            positionsList.Clear();
            positionsList.AddRange(positions);
        }
    }

    /// <summary>
    /// Clear all data from the repository.
    /// </summary>
    public void Clear()
    {
        _portfolios.Clear();
        _positions.Clear();
        _transactions.Clear();
        _nextPortfolioId = 1;
        _nextPositionId = 1;
        _nextTransactionId = 1;
    }

    /// <summary>
    /// Seed a portfolio directly for testing.
    /// </summary>
    public void SeedPortfolio(Portfolio portfolio)
    {
        if (portfolio.Id == 0)
        {
            portfolio.Id = _nextPortfolioId++;
        }

        _portfolios[portfolio.Id] = portfolio;

        // Seed positions if any
        foreach (Position position in portfolio.Positions)
        {
            if (position.Id == 0)
            {
                position.Id = _nextPositionId++;
            }

            position.PortfolioId = portfolio.Id;
            _positions[position.Id] = position;
        }
    }

    /// <summary>
    /// Get all portfolios without loading positions (for internal test verification).
    /// </summary>
    public Dictionary<int, Portfolio> GetAllPortfoliosRaw()
    {
        return new Dictionary<int, Portfolio>(_portfolios);
    }

    /// <summary>
    /// Get all positions (for internal test verification).
    /// </summary>
    public Dictionary<int, Position> GetAllPositionsRaw()
    {
        return new Dictionary<int, Position>(_positions);
    }

    /// <summary>
    /// Get all transactions (for internal test verification).
    /// </summary>
    public List<PortfolioCashTransaction> GetAllTransactionsRaw()
    {
        return new List<PortfolioCashTransaction>(_transactions);
    }

    #endregion
}
