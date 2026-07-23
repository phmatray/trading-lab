// <copyright file="JobScheduler.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.BackgroundJobs;

/// <summary>
/// Base class for scheduled background jobs.
/// </summary>
public abstract class JobScheduler : BackgroundService
{
    private readonly ILogger<JobScheduler> _logger;
    private readonly TimeSpan _interval;
    private readonly IJob _job;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobScheduler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="job">Job to execute.</param>
    /// <param name="interval">Execution interval.</param>
    protected JobScheduler(
        ILogger<JobScheduler> logger,
        IJob job,
        TimeSpan interval)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _job = job ?? throw new ArgumentNullException(nameof(job));
        _interval = interval;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting job scheduler for {JobName} with interval {Interval}", _job.JobName, _interval);

        using var timer = new PeriodicTimer(_interval);

        try
        {
            // Execute immediately on start
            await ExecuteJobAsync(stoppingToken);

            // Then execute on timer intervals
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ExecuteJobAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Job scheduler for {JobName} is stopping", _job.JobName);
        }
    }

    private async Task ExecuteJobAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Executing job: {JobName}", _job.JobName);
            await _job.ExecuteAsync(cancellationToken);
            _logger.LogDebug("Completed job: {JobName}", _job.JobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing job {JobName}: {Message}", _job.JobName, ex.Message);
        }
    }
}
