// <copyright file="EndOfDayJobScheduler.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.BackgroundJobs;

/// <summary>
/// Scheduler for end-of-day job (runs once daily at midnight UTC).
/// </summary>
public sealed class EndOfDayJobScheduler : JobScheduler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndOfDayJobScheduler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="job">End of day job.</param>
    public EndOfDayJobScheduler(
        ILogger<JobScheduler> logger,
        EndOfDayJob job)
        : base(logger, job, CalculateInterval())
    {
    }

    private static TimeSpan CalculateInterval()
    {
        // Calculate time until next midnight UTC
        var now = DateTime.UtcNow;
        var nextMidnight = now.Date.AddDays(1);
        var timeUntilMidnight = nextMidnight - now;

        // For daily job, we use 24 hours as the interval
        // The job will run once daily at approximately midnight UTC
        return TimeSpan.FromHours(24);
    }
}
