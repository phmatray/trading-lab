// <copyright file="ITradingCalendar.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service interface for checking trading calendar (market hours, holidays, trading days).
/// </summary>
public interface ITradingCalendar
{
    /// <summary>
    /// Checks if the market is open on a given date.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if the market is open on this date.</returns>
    bool IsMarketOpen(DateTime date);

    /// <summary>
    /// Checks if a given date is a trading day (weekday, not a holiday).
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if the date is a trading day.</returns>
    bool IsTradingDay(DateTime date);

    /// <summary>
    /// Gets the next trading day after a given date.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <returns>The next trading day.</returns>
    DateTime GetNextTradingDay(DateTime date);

    /// <summary>
    /// Checks if the current time is within market hours.
    /// </summary>
    /// <param name="currentTime">The current time to check.</param>
    /// <returns>True if within market hours.</returns>
    bool IsWithinMarketHours(DateTime currentTime);
}
