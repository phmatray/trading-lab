// <copyright file="BacktestExecutionWorker.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Web.Services;

namespace TradingBot.Web.Workers;

/// <summary>
/// Background service that processes backtest execution requests from the task queue.
/// </summary>
public class BacktestExecutionWorker : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<BacktestExecutionWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="BacktestExecutionWorker"/> class.
    /// </summary>
    /// <param name="taskQueue">The background task queue.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceProvider">The service provider for creating scoped services.</param>
    public BacktestExecutionWorker(
        IBackgroundTaskQueue taskQueue,
        ILogger<BacktestExecutionWorker> logger,
        IServiceProvider serviceProvider)
    {
        _taskQueue = taskQueue;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Executes the background worker, dequeuing and processing tasks.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token to stop the worker.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BacktestExecutionWorker started and listening for tasks");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                _logger.LogInformation("Dequeued a backtest execution work item");

                try
                {
                    await workItem(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing backtest work item");
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                _logger.LogInformation("BacktestExecutionWorker stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in BacktestExecutionWorker");
            }
        }

        _logger.LogInformation("BacktestExecutionWorker stopped");
    }
}
