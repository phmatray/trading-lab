// <copyright file="PortfolioService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Text;
using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for managing portfolio history and positions.
/// </summary>
public sealed class PortfolioService : IPortfolioService
{
    private readonly IPortfolioManager _portfolioManager;
    private readonly ILogger<PortfolioService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PortfolioService"/> class.
    /// </summary>
    /// <param name="portfolioManager">The portfolio manager.</param>
    /// <param name="logger">The logger instance.</param>
    public PortfolioService(
        IPortfolioManager portfolioManager,
        ILogger<PortfolioService> logger)
    {
        _portfolioManager = portfolioManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Trade>> GetTradeHistoryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? symbol = null,
        string? strategy = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Loading trade history: StartDate={StartDate}, EndDate={EndDate}, Symbol={Symbol}, Strategy={Strategy}",
                startDate,
                endDate,
                symbol,
                strategy);

            var trades = await _portfolioManager.GetTradeHistoryAsync(
                startDate,
                endDate,
                symbol,
                strategy,
                cancellationToken);

            _logger.LogInformation("Trade history loaded: {Count} trades", trades.Count());

            return trades;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading trade history");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Position>> GetOpenPositionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading open positions");

            var positions = await _portfolioManager.GetPositionsAsync(cancellationToken);

            _logger.LogInformation("Open positions loaded: {Count} positions", positions.Count);

            return positions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading open positions");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ClosePositionAsync(Guid positionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Closing position: {PositionId}", positionId);

            // Get the position to find its symbol
            var positions = await _portfolioManager.GetPositionsAsync(cancellationToken);
            var position = positions.FirstOrDefault(p => p.Id == positionId);

            if (position == null)
            {
                _logger.LogWarning("Position not found: {PositionId}", positionId);
                return false;
            }

            var result = await _portfolioManager.ClosePositionAsync(position.Symbol, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Position closed successfully: {PositionId} ({Symbol})", positionId, position.Symbol);
            }
            else
            {
                _logger.LogWarning("Failed to close position: {PositionId} ({Symbol})", positionId, position.Symbol);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing position: {PositionId}", positionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> ExportTradeHistoryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? symbol = null,
        string? strategy = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting trade history to CSV");

            var trades = await GetTradeHistoryAsync(
                startDate,
                endDate,
                symbol,
                strategy,
                cancellationToken);

            // Build CSV content
            var csv = new StringBuilder();
            csv.AppendLine(
                "Symbol,Side,Quantity,Entry Price,Exit Price,Entry Time,Exit Time,Duration (days),Realized P&L,P&L %,Commission,Strategy");

            foreach (var trade in trades.OrderByDescending(t => t.ExitTime))
            {
                var duration = (trade.ExitTime - trade.EntryTime).TotalDays;
                var pnlPercent = trade.EntryPrice != 0
                    ? ((trade.ExitPrice - trade.EntryPrice) / trade.EntryPrice) * 100m
                    : 0m;

                csv.AppendLine(
                    $"\"{trade.Symbol}\"," +
                    $"\"{trade.Side.Name}\"," +
                    $"{trade.Quantity:F2}," +
                    $"{trade.EntryPrice:F2}," +
                    $"{trade.ExitPrice:F2}," +
                    $"\"{trade.EntryTime:yyyy-MM-dd HH:mm:ss}\"," +
                    $"\"{trade.ExitTime:yyyy-MM-dd HH:mm:ss}\"," +
                    $"{duration:F2}," +
                    $"{trade.RealizedPnL:F2}," +
                    $"{pnlPercent:F2}," +
                    $"{trade.Commission:F2}," +
                    $"\"{trade.StrategyName}\"");
            }

            _logger.LogInformation("Trade history exported: {Count} trades", trades.Count());

            return csv.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting trade history");
            throw;
        }
    }
}
