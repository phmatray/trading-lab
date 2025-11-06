// <copyright file="DrawdownPeriod.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Analytics;

/// <summary>
/// Represents a drawdown period with start, end, and recovery information.
/// </summary>
public sealed class DrawdownPeriod
{
    /// <summary>
    /// Gets or sets the start date of the drawdown.
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date (trough) of the drawdown.
    /// </summary>
    public required DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the recovery date (when equity returned to peak).
    /// </summary>
    public DateTime? RecoveryDate { get; set; }

    /// <summary>
    /// Gets or sets the peak equity before drawdown.
    /// </summary>
    public required decimal PeakEquity { get; set; }

    /// <summary>
    /// Gets or sets the trough equity (lowest point).
    /// </summary>
    public required decimal TroughEquity { get; set; }

    /// <summary>
    /// Gets or sets the maximum drawdown percentage.
    /// </summary>
    public decimal MaxDrawdownPercent { get; set; }

    /// <summary>
    /// Gets or sets the duration of the drawdown in days.
    /// </summary>
    public int DurationDays { get; set; }

    /// <summary>
    /// Gets or sets the recovery duration in days (if recovered).
    /// </summary>
    public int? RecoveryDays { get; set; }

    /// <summary>
    /// Gets a value indicating whether this drawdown has recovered.
    /// </summary>
    public bool IsRecovered => RecoveryDate.HasValue;
}
