// <copyright file="PortfolioManager.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Engine;

/// <summary>
/// Service for managing portfolio positions and account information.
/// </summary>
public sealed class PortfolioManager : IPortfolioManager
{
    private readonly ILogger<PortfolioManager> _logger;
    private readonly IMarketDataService _marketDataService;
    private readonly Account _account;
    private readonly List<Position> _positions;
    private readonly List<Trade> _tradeHistory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="PortfolioManager"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="marketDataService">Market data service.</param>
    public PortfolioManager(
        ILogger<PortfolioManager> logger,
        IMarketDataService marketDataService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _marketDataService = marketDataService ?? throw new ArgumentNullException(nameof(marketDataService));

        _account = new Account
        {
            AccountId = "demo-account",
            Equity = 100000m,
            Cash = 100000m,
            PositionValue = 0m,
            BuyingPower = 100000m,
            Leverage = 1m,
            UnrealizedPnL = 0m,
            RealizedPnL = 0m,
        };

        _positions = new List<Position>();
        _tradeHistory = new List<Trade>();
    }

    /// <inheritdoc/>
    public async Task<Account> GetAccountAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await UpdateAccountMetricsAsync(cancellationToken);
            return _account;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Position>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Update current prices for all positions
            foreach (var position in _positions)
            {
                try
                {
                    var quote = await _marketDataService.GetQuoteAsync(position.Symbol, cancellationToken);
                    position.CurrentPrice = quote.Price;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update price for {Symbol}", position.Symbol);
                }
            }

            return _positions.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Position?> GetPositionAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var position = _positions.FirstOrDefault(p =>
                p.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

            if (position != null)
            {
                try
                {
                    var quote = await _marketDataService.GetQuoteAsync(position.Symbol, cancellationToken);
                    position.CurrentPrice = quote.Price;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update price for {Symbol}", position.Symbol);
                }
            }

            return position;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Trade>> GetTradeHistoryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? symbol = null,
        string? strategyName = null,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var query = _tradeHistory.AsEnumerable();

            if (startDate.HasValue)
            {
                query = query.Where(t => t.EntryTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.ExitTime <= endDate.Value);
            }

            if (!string.IsNullOrEmpty(symbol))
            {
                query = query.Where(t => t.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(strategyName))
            {
                query = query.Where(t => t.StrategyName.Equals(strategyName, StringComparison.OrdinalIgnoreCase));
            }

            return query.OrderByDescending(t => t.ExitTime).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ClosePositionAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var position = _positions.FirstOrDefault(p =>
                p.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

            if (position == null)
            {
                _logger.LogWarning("Position not found for symbol: {Symbol}", symbol);
                return false;
            }

            // Get current price
            var quote = await _marketDataService.GetQuoteAsync(position.Symbol, cancellationToken);
            position.CurrentPrice = quote.Price;

            // Create trade record
            var trade = new Trade
            {
                Id = Guid.NewGuid(),
                Symbol = position.Symbol,
                Side = position.Side,
                Quantity = position.Quantity,
                EntryPrice = position.EntryPrice,
                ExitPrice = position.CurrentPrice,
                EntryTime = position.OpenedAt,
                ExitTime = DateTime.UtcNow,
                Commission = 0m, // Simplified for now
                StrategyName = position.StrategyName,
            };

            _tradeHistory.Add(trade);
            _positions.Remove(position);

            // Update account
            var pnl = position.UnrealizedPnL;
            _account.RealizedPnL += pnl;
            _account.Cash += (position.Quantity * position.CurrentPrice) + pnl;

            _logger.LogInformation(
                "Closed position: {Symbol}, P&L: {PnL:C}",
                symbol,
                pnl);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<int> CloseAllPositionsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var count = 0;
            var positionsToClose = _positions.ToList();

            foreach (var position in positionsToClose)
            {
                _lock.Release(); // Release lock before async call
                var closed = await ClosePositionAsync(position.Symbol, cancellationToken);
                await _lock.WaitAsync(cancellationToken); // Re-acquire lock

                if (closed)
                {
                    count++;
                }
            }

            _logger.LogInformation("Closed {Count} positions", count);
            return count;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<PerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var positions = await GetPositionsAsync(cancellationToken);
            var totalPnL = _account.RealizedPnL + positions.Sum(p => p.UnrealizedPnL);
            var totalReturn = _account.Equity > 0 ? (totalPnL / 100000m) * 100m : 0m;

            var winningTrades = _tradeHistory.Count(t => t.RealizedPnL > 0);
            var losingTrades = _tradeHistory.Count(t => t.RealizedPnL < 0);
            var totalTrades = _tradeHistory.Count;

            var grossWins = winningTrades > 0
                ? _tradeHistory.Where(t => t.RealizedPnL > 0).Sum(t => t.RealizedPnL)
                : 0m;
            var grossLosses = losingTrades > 0
                ? Math.Abs(_tradeHistory.Where(t => t.RealizedPnL < 0).Sum(t => t.RealizedPnL))
                : 0m;

            return new PerformanceMetrics
            {
                TotalReturn = totalReturn,
                AnnualizedReturn = 0m, // TODO: Calculate based on time period
                SharpeRatio = 0m, // TODO: Calculate from returns
                SortinoRatio = 0m, // TODO: Calculate from returns
                CalmarRatio = 0m, // TODO: Calculate from returns
                MaxDrawdown = 0m, // TODO: Track historical drawdowns
                ProfitFactor = grossLosses > 0 ? grossWins / grossLosses : 0m,
                TotalTrades = totalTrades,
                WinningTrades = winningTrades,
                LosingTrades = losingTrades,
                AverageWin = winningTrades > 0
                    ? _tradeHistory.Where(t => t.RealizedPnL > 0).Average(t => t.RealizedPnL)
                    : 0m,
                AverageLoss = losingTrades > 0
                    ? _tradeHistory.Where(t => t.RealizedPnL < 0).Average(t => t.RealizedPnL)
                    : 0m,
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task UpdateAccountMetricsAsync(CancellationToken cancellationToken)
    {
        // Update current prices for all positions (lock already held by caller)
        foreach (var position in _positions)
        {
            try
            {
                var quote = await _marketDataService.GetQuoteAsync(position.Symbol, cancellationToken);
                position.CurrentPrice = quote.Price;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update price for {Symbol}", position.Symbol);
            }
        }

        _account.PositionValue = _positions.Sum(p => p.Quantity * p.CurrentPrice);
        _account.UnrealizedPnL = _positions.Sum(p => p.UnrealizedPnL);
        _account.Equity = _account.Cash + _account.PositionValue + _account.UnrealizedPnL;
        _account.BuyingPower = _account.Cash * _account.Leverage;
    }
}
