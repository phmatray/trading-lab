using System.Globalization;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Services;

/// <summary>
/// Domain service for aggregating daily OHLC bars into higher timeframes (weekly, monthly).
/// Uses ISO 8601 week calculation for consistent week boundaries.
/// </summary>
public class TimeframeAggregator
{
    /// <summary>
    /// Aggregates daily historical prices into weekly bars.
    /// Week boundaries determined by ISO 8601 (Monday start).
    /// </summary>
    /// <param name="dailyPrices">Daily OHLC data, must be sorted chronologically ascending.</param>
    /// <returns>Weekly OHLC bars with aggregated volume.</returns>
    public HistoricalPrice[] AggregateToWeekly(IReadOnlyList<HistoricalPrice> dailyPrices)
    {
        ArgumentNullException.ThrowIfNull(dailyPrices);
        if (dailyPrices.Count == 0)
        {
            return [];
        }

        List<HistoricalPrice> weeklyBars = [];
        List<HistoricalPrice> currentWeekBars = [];
        int? currentWeekNumber = null;
        int? currentYear = null;

        foreach (HistoricalPrice dailyBar in dailyPrices)
        {
            (int year, int week) = GetIsoWeekNumber(dailyBar.DateTime);

            // New week detected
            if (currentWeekNumber != week || currentYear != year)
            {
                if (currentWeekBars.Count > 0)
                {
                    weeklyBars.Add(AggregateWeekBars(currentWeekBars));
                    currentWeekBars.Clear();
                }

                currentWeekNumber = week;
                currentYear = year;
            }

            currentWeekBars.Add(dailyBar);
        }

        // Aggregate final week
        if (currentWeekBars.Count > 0)
        {
            weeklyBars.Add(AggregateWeekBars(currentWeekBars));
        }

        return [.. weeklyBars];
    }

    private static (int year, int weekNumber) GetIsoWeekNumber(DateTime date)
    {
        // ISO 8601: Week 1 is the week with the first Thursday of the year
        DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            date = date.AddDays(3);
        }

        int weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            date,
            CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);

        // Handle year boundary edge case
        int year = date.Year;
        if (weekNumber == 1 && date.Month == 12)
        {
            year++;
        }
        else if (weekNumber >= 52 && date.Month == 1)
        {
            year--;
        }

        return (year, weekNumber);
    }

    private static HistoricalPrice AggregateWeekBars(List<HistoricalPrice> bars)
    {
        // Weekly bar:
        // - DateTime: last day of the week (Friday or last available)
        // - Open: first bar's open
        // - High: max of all highs
        // - Low: min of all lows
        // - Close: last bar's close
        // - Volume: sum of volumes

        decimal? highestHigh = bars.Max(b => b.High);
        decimal? lowestLow = bars.Where(b => b.Low.HasValue).Min(b => b.Low);
        long? totalVolume = bars.Sum(b => b.Volume ?? 0);

        return new HistoricalPrice
        {
            Ticker = bars[0].Ticker,
            ISIN = bars[0].ISIN,
            DateTime = bars[^1].DateTime, // Last bar of week
            Open = bars[0].Open,
            High = highestHigh,
            Low = lowestLow,
            Close = bars[^1].Close,
            AdjustedClose = bars[^1].AdjustedClose,
            Volume = totalVolume == 0 ? null : totalVolume
        };
    }
}
