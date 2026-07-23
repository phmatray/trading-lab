// <copyright file="IJob.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Infrastructure.BackgroundJobs;

/// <summary>
/// Interface for background jobs.
/// </summary>
public interface IJob
{
    /// <summary>
    /// Gets the job name.
    /// </summary>
    string JobName { get; }

    /// <summary>
    /// Executes the job logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(CancellationToken cancellationToken);
}
