// <copyright file="EquityPoint.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Analytics;

/// <summary>
/// Represents a point on the equity curve with associated metrics.
/// Consolidated from Models/Portfolio/EquityPoint.cs and Models/Analytics/EquityPoint.cs.
/// </summary>
public sealed class EquityPoint
{
    /// <summary>
    /// Gets or sets the timestamp of this equity point.
    /// </summary>
    public required DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the total equity at this point.
    /// </summary>
    public required decimal Equity { get; set; }

    /// <summary>
    /// Gets or sets the cumulative return percentage.
    /// </summary>
    public decimal CumulativeReturn { get; set; }

    /// <summary>
    /// Gets or sets the drawdown percentage at this point.
    /// </summary>
    public decimal Drawdown { get; set; }

    /// <summary>
    /// Gets or sets the peak equity up to this point.
    /// </summary>
    public decimal Peak { get; set; }

    /// <summary>
    /// Gets or sets the return percentage from initial capital.
    /// </summary>
    public decimal ReturnPercent { get; set; }
}
