// <copyright file="PortfolioService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Text;
using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Web.Models;

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
    public async Task<PortfolioHistoryResult> GetTradeHistoryAsync(
        PortfolioHistoryFilter filter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Loading trade history with filter: StartDate={StartDate}, EndDate={EndDate}, Symbol={Symbol}, Strategy={Strategy}",
                filter.StartDate,
                filter.EndDate,
                filter.Symbol,
                filter.StrategyName);

            // Get all trades matching the filter criteria
            var allTrades = await _portfolioManager.GetTradeHistoryAsync(
                filter.StartDate,
                filter.EndDate,
                filter.Symbol,
                filter.StrategyName,
                cancellationToken);

            // Apply additional filters (P&L, Side)
            var filteredTrades = allTrades.AsEnumerable();

            if (filter.MinPnL.HasValue)
            {
                filteredTrades = filteredTrades.Where(t => t.RealizedPnL >= filter.MinPnL.Value);
            }

            if (filter.MaxPnL.HasValue)
            {
                filteredTrades = filteredTrades.Where(t => t.RealizedPnL <= filter.MaxPnL.Value);
            }

            if (filter.Side != null)
            {
                filteredTrades = filteredTrades.Where(t => t.Side == filter.Side);
            }

            var tradesList = filteredTrades.ToList();
            var totalCount = tradesList.Count;

            // Apply pagination
            var paginatedTrades = tradesList
                .OrderByDescending(t => t.ExitTime)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var result = new PortfolioHistoryResult
            {
                Trades = paginatedTrades,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
            };

            _logger.LogInformation(
                "Trade history loaded: {Count} trades, page {PageNumber}/{TotalPages}",
                totalCount,
                result.PageNumber,
                result.TotalPages);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading trade history");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ClosePositionAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Closing position for symbol: {Symbol}", symbol);

            var result = await _portfolioManager.ClosePositionAsync(symbol, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Position closed successfully: {Symbol}", symbol);
            }
            else
            {
                _logger.LogWarning("Position not found for symbol: {Symbol}", symbol);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing position: {Symbol}", symbol);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> ExportTradeHistoryAsync(
        PortfolioHistoryFilter filter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting trade history to CSV");

            // Get all trades matching the filter (no pagination for export)
            var historyResult = await GetTradeHistoryAsync(
                new PortfolioHistoryFilter
                {
                    StartDate = filter.StartDate,
                    EndDate = filter.EndDate,
                    Symbol = filter.Symbol,
                    StrategyName = filter.StrategyName,
                    MinPnL = filter.MinPnL,
                    MaxPnL = filter.MaxPnL,
                    Side = filter.Side,
                    PageNumber = 1,
                    PageSize = int.MaxValue, // Get all trades
                },
                cancellationToken);

            var trades = historyResult.Trades;

            // Build CSV content
            var csv = new StringBuilder();
            csv.AppendLine("Symbol,Side,Quantity,Entry Price,Exit Price,Entry Time,Exit Time,Duration,Realized P&L,Realized P&L %,Commission,Strategy");

            foreach (var trade in trades)
            {
                csv.AppendLine(
                    $"{trade.Symbol}," +
                    $"{trade.Side}," +
                    $"{trade.Quantity}," +
                    $"{trade.EntryPrice:F2}," +
                    $"{trade.ExitPrice:F2}," +
                    $"{trade.EntryTime:yyyy-MM-dd HH:mm:ss}," +
                    $"{trade.ExitTime:yyyy-MM-dd HH:mm:ss}," +
                    $"{trade.Duration}," +
                    $"{trade.RealizedPnL:F2}," +
                    $"{trade.RealizedPnLPercent:F2}," +
                    $"{trade.Commission:F2}," +
                    $"{trade.StrategyName}");
            }

            var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());

            _logger.LogInformation("Trade history exported: {Count} trades", trades.Count);

            return csvBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting trade history");
            throw;
        }
    }
}
