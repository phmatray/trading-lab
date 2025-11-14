// <copyright file="EquityPoint.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Portfolio;

/// <summary>
/// Represents a point in the equity curve.
/// </summary>
public sealed record EquityPoint
{
    /// <summary>
    /// Gets the timestamp (UTC).
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the equity value at this point.
    /// </summary>
    public required decimal Equity { get; init; }

    /// <summary>
    /// Gets the cumulative return percentage.
    /// </summary>
    public required decimal CumulativeReturn { get; init; }
}
