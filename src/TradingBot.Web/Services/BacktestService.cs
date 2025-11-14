// <copyright file="BacktestService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Backtest;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for retrieving backtest results.
/// </summary>
public sealed class BacktestService : IBacktestService
{
    private readonly ILogger<BacktestService> _logger;

    // Note: In a real implementation, this would use a repository to fetch backtest results from database
    // For now, we'll return empty lists as backtest results are typically generated via CLI
    private readonly List<BacktestResult> _backtestResults = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BacktestService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public BacktestService(ILogger<BacktestService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<List<BacktestResult>> GetBacktestResultsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading backtest results");

            // In a real implementation, this would query a repository
            // For now, return empty list as backtests are run via CLI
            var results = _backtestResults
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            _logger.LogInformation("Loaded {Count} backtest results", results.Count);

            return Task.FromResult(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading backtest results");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<BacktestResult?> GetBacktestByIdAsync(string backtestId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading backtest result: {BacktestId}", backtestId);

            // In a real implementation, this would query a repository
            var result = _backtestResults.FirstOrDefault(b => b.BacktestId == backtestId);

            if (result == null)
            {
                _logger.LogWarning("Backtest result not found: {BacktestId}", backtestId);
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading backtest result: {BacktestId}", backtestId);
            throw;
        }
    }
}
