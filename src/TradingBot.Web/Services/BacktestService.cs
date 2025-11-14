// <copyright file="BacktestService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using TradingBot.Analytics;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Backtest;
using TradingBot.Core.Models.Trading;
using TradingBot.Web.Hubs;
using TradingBot.Web.Models;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for running backtests and managing backtest results.
/// </summary>
public sealed class BacktestService : IBacktestService
{
    private readonly ILogger<BacktestService> _logger;
    private readonly IBacktestResultRepository _backtestResultRepository;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<TradingHub, ITradingClient> _hubContext;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _runningBacktests = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BacktestService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="backtestResultRepository">The backtest result repository.</param>
    /// <param name="taskQueue">The background task queue.</param>
    /// <param name="serviceProvider">The service provider for creating scoped services.</param>
    /// <param name="hubContext">The SignalR hub context.</param>
    public BacktestService(
        ILogger<BacktestService> logger,
        IBacktestResultRepository backtestResultRepository,
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        IHubContext<TradingHub, ITradingClient> hubContext)
    {
        _logger = logger;
        _backtestResultRepository = backtestResultRepository;
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    /// <inheritdoc/>
    public async Task<List<BacktestResult>> GetBacktestResultsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading backtest results from repository");
            return await _backtestResultRepository.GetAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading backtest results");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<BacktestResult?> GetBacktestByIdAsync(string backtestId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading backtest result: {BacktestId}", backtestId);
            return await _backtestResultRepository.GetByIdAsync(backtestId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading backtest result: {BacktestId}", backtestId);
            throw;
        }
    }

    /// <summary>
    /// Runs a backtest asynchronously and saves the results.
    /// </summary>
    /// <param name="request">The backtest configuration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated backtest ID.</returns>
    public async Task<string> RunBacktestAsync(BacktestRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.StrategyName))
            {
                throw new ArgumentException("Strategy name is required", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Symbol))
            {
                throw new ArgumentException("Symbol is required", nameof(request));
            }

            if (request.EndDate <= request.StartDate)
            {
                throw new ArgumentException("End date must be after start date", nameof(request));
            }

            if (request.InitialCapital <= 0)
            {
                throw new ArgumentException("Initial capital must be positive", nameof(request));
            }

            // Generate unique backtest ID
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var backtestId = $"bt_{request.StrategyName}_{request.Symbol}_{timestamp}";

            _logger.LogInformation(
                "Queueing backtest {BacktestId} for strategy {Strategy} on symbol {Symbol} from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}",
                backtestId,
                request.StrategyName,
                request.Symbol,
                request.StartDate,
                request.EndDate);

            // Create cancellation token source for this backtest
            var cts = new CancellationTokenSource();
            _runningBacktests[backtestId] = cts;

            // Queue the backtest execution as a background task
            await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
            {
                await ExecuteBacktestAsync(backtestId, request, cts.Token);
            });

            return backtestId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing backtest for strategy {Strategy}", request.StrategyName);
            throw;
        }
    }

    /// <summary>
    /// Cancels a running backtest.
    /// </summary>
    /// <param name="backtestId">The ID of the backtest to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if backtest was cancelled successfully, false if not found.</returns>
    public Task<bool> CancelBacktestAsync(string backtestId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting to cancel backtest {BacktestId}", backtestId);

            if (_runningBacktests.TryRemove(backtestId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _logger.LogInformation("Successfully cancelled backtest {BacktestId}", backtestId);
                return Task.FromResult(true);
            }

            _logger.LogWarning("Backtest {BacktestId} not found in running backtests", backtestId);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling backtest {BacktestId}", backtestId);
            throw;
        }
    }

    /// <summary>
    /// Deletes a backtest result.
    /// </summary>
    /// <param name="backtestId">The ID of the backtest to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if backtest was deleted successfully, false if not found.</returns>
    public async Task<bool> DeleteBacktestAsync(string backtestId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting backtest {BacktestId}", backtestId);
            return await _backtestResultRepository.DeleteAsync(backtestId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backtest {BacktestId}", backtestId);
            throw;
        }
    }

    /// <summary>
    /// Exports a backtest's trade list to CSV format.
    /// </summary>
    /// <param name="backtestId">The ID of the backtest whose trades to export.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CSV-formatted string with trade data, or empty string if backtest not found.</returns>
    public async Task<string> ExportBacktestTradesToCsvAsync(string backtestId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting trades to CSV for backtest {BacktestId}", backtestId);

            var result = await _backtestResultRepository.GetByIdAsync(backtestId, cancellationToken);
            if (result == null)
            {
                _logger.LogWarning("Backtest {BacktestId} not found for export", backtestId);
                return string.Empty;
            }

            // Parse trades from JSON
            var trades = JsonSerializer.Deserialize<List<Trade>>(result.TradesJson) ?? new List<Trade>();

            // Build CSV
            var csv = new StringBuilder();
            csv.AppendLine("Symbol,Side,EntryTime,EntryPrice,ExitTime,ExitPrice,Quantity,RealizedPnL,Commission,StrategyName");

            foreach (var trade in trades)
            {
                csv.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0},{1},{2:yyyy-MM-dd HH:mm:ss},{3:F2},{4:yyyy-MM-dd HH:mm:ss},{5:F2},{6:F4},{7:F2},{8:F2},{9}",
                    trade.Symbol,
                    trade.Side.Name,
                    trade.EntryTime,
                    trade.EntryPrice,
                    trade.ExitTime,
                    trade.ExitPrice,
                    trade.Quantity,
                    trade.RealizedPnL,
                    trade.Commission,
                    trade.StrategyName));
            }

            _logger.LogInformation("Exported {Count} trades for backtest {BacktestId}", trades.Count, backtestId);
            return csv.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting trades for backtest {BacktestId}", backtestId);
            throw;
        }
    }

    private async Task ExecuteBacktestAsync(string backtestId, BacktestRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting backtest execution for {BacktestId}", backtestId);

            // Notify progress: 0%
            await _hubContext.Clients.All.OnBacktestProgress(backtestId, 0, "Initializing backtest...");

            // Create a new scope for scoped services
            using var scope = _serviceProvider.CreateScope();
            var backtestingEngine = scope.ServiceProvider.GetRequiredService<IBacktestingEngine>();
            var strategyEngine = scope.ServiceProvider.GetRequiredService<IStrategyEngine>();

            // Notify progress: 10%
            await _hubContext.Clients.All.OnBacktestProgress(backtestId, 10, "Loading historical data...");

            // Find the strategy
            var strategy = await strategyEngine.GetStrategyAsync(request.StrategyName, cancellationToken);
            if (strategy == null)
            {
                throw new InvalidOperationException($"Strategy '{request.StrategyName}' not found");
            }

            // Notify progress: 30%
            await _hubContext.Clients.All.OnBacktestProgress(backtestId, 30, "Running backtest simulation...");

            // Create backtest configuration
            var configuration = new BacktestConfiguration
            {
                BacktestId = backtestId,
                StrategyName = request.StrategyName,
                Symbol = request.Symbol,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                InitialCapital = request.InitialCapital,
                CommissionPerTrade = 1.0m,
                SlippagePercent = 0.1m,
                EnableTransactionCosts = true,
            };

            // Execute backtest using the backtest engine
            var backtestResult = await backtestingEngine.RunBacktestAsync(configuration, cancellationToken);

            // Set the backtest ID
            backtestResult.BacktestId = backtestId;
            backtestResult.CreatedAt = DateTime.UtcNow;

            // Notify progress: 80%
            await _hubContext.Clients.All.OnBacktestProgress(backtestId, 80, "Calculating performance metrics...");

            // Notify progress: 90%
            await _hubContext.Clients.All.OnBacktestProgress(backtestId, 90, "Saving results...");

            // Save result to database
            await _backtestResultRepository.SaveAsync(backtestResult, cancellationToken);

            // Notify progress: 100%
            await _hubContext.Clients.All.OnBacktestProgress(backtestId, 100, "Backtest completed");

            // Notify completion
            await _hubContext.Clients.All.OnBacktestCompleted(backtestId, backtestResult);

            _logger.LogInformation(
                "Backtest {BacktestId} completed successfully. Total Return: {TotalReturn:F2}%, Sharpe Ratio: {SharpeRatio:F2}, Max Drawdown: {MaxDrawdown:F2}%",
                backtestId,
                backtestResult.TotalReturn,
                backtestResult.SharpeRatio,
                backtestResult.MaxDrawdown);

            // Clean up cancellation token
            if (_runningBacktests.TryRemove(backtestId, out var cts))
            {
                cts.Dispose();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Backtest {BacktestId} was cancelled", backtestId);
            await _hubContext.Clients.All.OnBacktestFailed(backtestId, "Backtest was cancelled by user");

            // Clean up cancellation token
            if (_runningBacktests.TryRemove(backtestId, out var cts))
            {
                cts.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing backtest {BacktestId}", backtestId);
            await _hubContext.Clients.All.OnBacktestFailed(backtestId, ex.Message);

            // Clean up cancellation token
            if (_runningBacktests.TryRemove(backtestId, out var cts))
            {
                cts.Dispose();
            }
        }
    }
}
