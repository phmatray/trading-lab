// <copyright file="RiskMonitoringJobScheduler.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.BackgroundJobs;

/// <summary>
/// Scheduler for risk monitoring job (runs every minute).
/// </summary>
public sealed class RiskMonitoringJobScheduler : JobScheduler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RiskMonitoringJobScheduler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="job">Risk monitoring job.</param>
    public RiskMonitoringJobScheduler(
        ILogger<JobScheduler> logger,
        RiskMonitoringJob job)
        : base(logger, job, TimeSpan.FromMinutes(1))
    {
    }
}
