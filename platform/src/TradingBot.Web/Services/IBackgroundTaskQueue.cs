// <copyright file="IBackgroundTaskQueue.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Services;

/// <summary>
/// Defines a queue for background task execution using Channel-based messaging.
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Queues a background work item for asynchronous execution.
    /// </summary>
    /// <param name="workItem">The async work function to execute.</param>
    /// <returns>A task representing the queueing operation.</returns>
    ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);

    /// <summary>
    /// Dequeues a background work item for execution.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop dequeuing.</param>
    /// <returns>The next work item to execute.</returns>
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}
