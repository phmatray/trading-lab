// <copyright file="TradingCalendar.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Interfaces;

namespace TradingBot.Infrastructure.Services;

/// <summary>
/// Service for checking trading calendar (market hours, holidays, trading days).
/// Implements US stock market calendar with major holidays.
/// </summary>
public sealed class TradingCalendar : ITradingCalendar
{
    // US market hours: 9:30 AM - 4:00 PM ET
    private static readonly TimeSpan MarketOpen = new(9, 30, 0);
    private static readonly TimeSpan MarketClose = new(16, 0, 0);

    // Major US stock market holidays (simplified - in production, use a comprehensive holiday calendar)
    private static readonly HashSet<DateTime> UsMarketHolidays2025 =
    [
        new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),   // New Year's Day
        new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc),  // Martin Luther King Jr. Day
        new DateTime(2025, 2, 17, 0, 0, 0, DateTimeKind.Utc),  // Presidents' Day
        new DateTime(2025, 4, 18, 0, 0, 0, DateTimeKind.Utc),  // Good Friday
        new DateTime(2025, 5, 26, 0, 0, 0, DateTimeKind.Utc),  // Memorial Day
        new DateTime(2025, 6, 19, 0, 0, 0, DateTimeKind.Utc),  // Juneteenth
        new DateTime(2025, 7, 4, 0, 0, 0, DateTimeKind.Utc),   // Independence Day
        new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc),   // Labor Day
        new DateTime(2025, 11, 27, 0, 0, 0, DateTimeKind.Utc), // Thanksgiving Day
        new DateTime(2025, 12, 25, 0, 0, 0, DateTimeKind.Utc), // Christmas Day
    ];

    /// <inheritdoc/>
    public bool IsMarketOpen(DateTime date)
    {
        return IsTradingDay(date);
    }

    /// <inheritdoc/>
    public bool IsTradingDay(DateTime date)
    {
        // Convert to date only (ignore time component)
        var dateOnly = date.Date;

        // Check if weekend
        if (dateOnly.DayOfWeek == DayOfWeek.Saturday || dateOnly.DayOfWeek == DayOfWeek.Sunday)
        {
            return false;
        }

        // Check if holiday
        if (UsMarketHolidays2025.Contains(dateOnly))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public DateTime GetNextTradingDay(DateTime date)
    {
        var nextDay = date.Date.AddDays(1);

        while (!IsTradingDay(nextDay))
        {
            nextDay = nextDay.AddDays(1);
        }

        return nextDay;
    }

    /// <inheritdoc/>
    public bool IsWithinMarketHours(DateTime currentTime)
    {
        // Check if it's a trading day first
        if (!IsTradingDay(currentTime))
        {
            return false;
        }

        // Convert to ET timezone (simplified - in production, use proper timezone conversion)
        // For now, assume input is already in ET or UTC with proper offset handling
        var timeOfDay = currentTime.TimeOfDay;

        // Check if within market hours (9:30 AM - 4:00 PM ET)
        return timeOfDay >= MarketOpen && timeOfDay <= MarketClose;
    }
}
