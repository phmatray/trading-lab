// <copyright file="EquityCurveDataPoint.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Models;

/// <summary>
/// Single point on equity curve chart.
/// </summary>
public sealed class EquityCurveDataPoint
{
    /// <summary>
    /// Gets or sets the timestamp for this data point.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the account equity at this timestamp.
    /// </summary>
    public decimal EquityValue { get; set; }
}
