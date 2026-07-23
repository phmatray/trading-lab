// <copyright file="MarketDataRefreshJobScheduler.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.BackgroundJobs;

/// <summary>
/// Scheduler for market data refresh job (runs every 5 minutes).
/// </summary>
public sealed class MarketDataRefreshJobScheduler : JobScheduler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketDataRefreshJobScheduler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="job">Market data refresh job.</param>
    public MarketDataRefreshJobScheduler(
        ILogger<JobScheduler> logger,
        MarketDataRefreshJob job)
        : base(logger, job, TimeSpan.FromMinutes(5))
    {
    }
}
