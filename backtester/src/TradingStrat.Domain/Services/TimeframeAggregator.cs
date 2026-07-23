using System.Globalization;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Services;

/// <summary>
/// Domain service for aggregating OHLC bars from lower timeframes to higher timeframes.
/// Supports both fixed-period aggregation (M5 -> M15, H1 -> H4) and calendar-based aggregation (daily -> weekly/monthly).
/// Uses ISO 8601 week calculation for consistent week boundaries.
/// </summary>
public class TimeFrameAggregator
{
    /// <summary>
    /// Aggregates lower timeframe bars to a higher target timeframe.
    /// Automatically selects the appropriate aggregation method based on the target timeframe.
    /// </summary>
    /// <param name="sourceBars">Source OHLC data, must be sorted chronologically ascending.</param>
    /// <param name="targetTimeFrame">Target timeframe to aggregate to (must be higher than source).</param>
    /// <returns>Aggregated OHLC bars in the target timeframe.</returns>
    /// <exception cref="ArgumentException">Thrown when source data is empty or target timeframe is not higher than source.</exception>
    public HistoricalPrice[] AggregateToTimeFrame(
        IReadOnlyList<HistoricalPrice> sourceBars,
        TimeFrame targetTimeFrame)
    {
        ArgumentNullException.ThrowIfNull(sourceBars);
        ArgumentNullException.ThrowIfNull(targetTimeFrame);

        if (sourceBars.Count == 0)
        {
            return [];
        }

        TimeFrameUnit sourceTimeFrame = sourceBars[0].TimeFrame;
        TimeFrame source = new() { Unit = sourceTimeFrame };

        if (source.ToMinutes() >= targetTimeFrame.ToMinutes())
        {
            throw new ArgumentException(
                $"Source timeframe {source} must be lower than target {targetTimeFrame}",
                nameof(targetTimeFrame));
        }

        // Route to appropriate aggregation method based on target timeframe
        return targetTimeFrame.Unit switch
        {
            TimeFrameUnit.W1 => AggregateToWeekly(sourceBars),
            TimeFrameUnit.Mn1 => AggregateToMonthly(sourceBars),
            _ => AggregateToFixedPeriod(sourceBars, targetTimeFrame)
        };
    }
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
            TimeFrame = TimeFrameUnit.W1,
            DateTime = bars[^1].DateTime, // Last bar of week
            Open = bars[0].Open,
            High = highestHigh,
            Low = lowestLow,
            Close = bars[^1].Close,
            AdjustedClose = bars[^1].AdjustedClose,
            Volume = totalVolume == 0 ? null : totalVolume
        };
    }

    /// <summary>
    /// Aggregates historical prices into monthly bars.
    /// Month boundaries determined by calendar months.
    /// </summary>
    /// <param name="sourceBars">Source OHLC data, must be sorted chronologically ascending.</param>
    /// <returns>Monthly OHLC bars with aggregated volume.</returns>
    public HistoricalPrice[] AggregateToMonthly(IReadOnlyList<HistoricalPrice> sourceBars)
    {
        ArgumentNullException.ThrowIfNull(sourceBars);
        if (sourceBars.Count == 0)
        {
            return [];
        }

        List<HistoricalPrice> monthlyBars = [];
        List<HistoricalPrice> currentMonthBars = [];
        int? currentMonth = null;
        int? currentYear = null;

        foreach (HistoricalPrice bar in sourceBars)
        {
            int month = bar.DateTime.Month;
            int year = bar.DateTime.Year;

            // New month detected
            if (currentMonth != month || currentYear != year)
            {
                if (currentMonthBars.Count > 0)
                {
                    monthlyBars.Add(AggregateMonthBars(currentMonthBars));
                    currentMonthBars.Clear();
                }

                currentMonth = month;
                currentYear = year;
            }

            currentMonthBars.Add(bar);
        }

        // Aggregate final month
        if (currentMonthBars.Count > 0)
        {
            monthlyBars.Add(AggregateMonthBars(currentMonthBars));
        }

        return [.. monthlyBars];
    }

    private static HistoricalPrice AggregateMonthBars(List<HistoricalPrice> bars)
    {
        decimal? highestHigh = bars.Max(b => b.High);
        decimal? lowestLow = bars.Where(b => b.Low.HasValue).Min(b => b.Low);
        long? totalVolume = bars.Sum(b => b.Volume ?? 0);

        return new HistoricalPrice
        {
            Ticker = bars[0].Ticker,
            ISIN = bars[0].ISIN,
            TimeFrame = TimeFrameUnit.Mn1,
            DateTime = bars[^1].DateTime, // Last bar of month
            Open = bars[0].Open,
            High = highestHigh,
            Low = lowestLow,
            Close = bars[^1].Close,
            AdjustedClose = bars[^1].AdjustedClose,
            Volume = totalVolume == 0 ? null : totalVolume
        };
    }

    /// <summary>
    /// Aggregates bars to a fixed-period timeframe (e.g., M5 -> M15, H1 -> H4).
    /// Groups bars by time intervals based on the target timeframe period.
    /// </summary>
    /// <param name="sourceBars">Source OHLC data, must be sorted chronologically ascending.</param>
    /// <param name="targetTimeFrame">Target timeframe to aggregate to.</param>
    /// <returns>Aggregated OHLC bars in the target timeframe.</returns>
    private HistoricalPrice[] AggregateToFixedPeriod(
        IReadOnlyList<HistoricalPrice> sourceBars,
        TimeFrame targetTimeFrame)
    {
        ArgumentNullException.ThrowIfNull(sourceBars);
        ArgumentNullException.ThrowIfNull(targetTimeFrame);

        if (sourceBars.Count == 0)
        {
            return [];
        }

        TimeFrameUnit sourceTimeFrame = sourceBars[0].TimeFrame;
        TimeFrame source = new() { Unit = sourceTimeFrame };

        int periodMultiplier = targetTimeFrame.GetPeriodMultiplier(source);

        List<HistoricalPrice> aggregatedBars = [];
        List<HistoricalPrice> currentPeriodBars = [];

        foreach (HistoricalPrice bar in sourceBars)
        {
            currentPeriodBars.Add(bar);

            // Check if we've accumulated enough bars for one target period
            if (currentPeriodBars.Count >= periodMultiplier)
            {
                aggregatedBars.Add(AggregatePeriodBars(currentPeriodBars, targetTimeFrame.Unit));
                currentPeriodBars.Clear();
            }
        }

        // Aggregate final partial period if any bars remain
        // (only if we have at least one complete bar)
        if (currentPeriodBars.Count > 0)
        {
            aggregatedBars.Add(AggregatePeriodBars(currentPeriodBars, targetTimeFrame.Unit));
        }

        return [.. aggregatedBars];
    }

    private static HistoricalPrice AggregatePeriodBars(List<HistoricalPrice> bars, TimeFrameUnit targetTimeFrame)
    {
        decimal? highestHigh = bars.Max(b => b.High);
        decimal? lowestLow = bars.Where(b => b.Low.HasValue).Min(b => b.Low);
        long? totalVolume = bars.Sum(b => b.Volume ?? 0);

        return new HistoricalPrice
        {
            Ticker = bars[0].Ticker,
            ISIN = bars[0].ISIN,
            TimeFrame = targetTimeFrame,
            DateTime = bars[^1].DateTime, // Last bar's timestamp
            Open = bars[0].Open,
            High = highestHigh,
            Low = lowestLow,
            Close = bars[^1].Close,
            AdjustedClose = bars[^1].AdjustedClose,
            Volume = totalVolume == 0 ? null : totalVolume
        };
    }
}
