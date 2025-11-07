// <copyright file="EquityDataPoint.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Models;

/// <summary>
/// Represents a single point on an equity curve chart.
/// </summary>
public sealed class EquityDataPoint
{
    /// <summary>
    /// Gets or sets the timestamp for this data point.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the equity value at this timestamp.
    /// </summary>
    public decimal EquityValue { get; set; }
}
