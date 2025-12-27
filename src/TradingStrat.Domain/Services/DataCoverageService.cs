using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Services;

/// <summary>
/// Domain service for calculating data coverage and detecting gaps in time series data.
/// Pure business logic with zero infrastructure dependencies.
/// Follows existing domain service patterns (PortfolioValuationService, PerformanceCalculator).
/// </summary>
public class DataCoverageService
{
    /// <summary>
    /// Detects gaps in time series data (missing days > 3).
    /// Returns a list of date gaps where data is missing.
    /// </summary>
    /// <param name="prices">The historical price data sorted by date.</param>
    /// <returns>List of date gaps with start date, end date, and days missing.</returns>
    public List<DateGap> DetectGaps(IReadOnlyList<HistoricalPrice> prices)
    {
        if (!prices.Any())
        {
            return new List<DateGap>();
        }

        var gaps = new List<DateGap>();
        var sortedPrices = prices.OrderBy(p => p.DateTime).ToList();

        for (int i = 1; i < sortedPrices.Count; i++)
        {
            DateTime previousDate = sortedPrices[i - 1].DateTime;
            DateTime currentDate = sortedPrices[i].DateTime;

            int daysBetween = (currentDate - previousDate).Days - 1;

            // If there's a gap of more than 3 days (excluding weekends), record it
            if (daysBetween > 3)
            {
                gaps.Add(new DateGap(
                    StartDate: previousDate.AddDays(1),
                    EndDate: currentDate.AddDays(-1),
                    DaysMissing: daysBetween
                ));
            }
        }

        return gaps;
    }

    /// <summary>
    /// Calculates coverage percentage (days with data / expected days).
    /// Returns percentage of days covered in the date range.
    /// </summary>
    /// <param name="daysCovered">Number of days with data available.</param>
    /// <param name="oldestDate">The oldest date in the dataset.</param>
    /// <param name="latestDate">The latest date in the dataset.</param>
    /// <returns>Coverage percentage (0-100).</returns>
    public decimal CalculateCoverage(
        int daysCovered,
        DateTime? oldestDate,
        DateTime? latestDate)
    {
        if (!oldestDate.HasValue || !latestDate.HasValue)
        {
            return 0m;
        }

        int expectedDays = (latestDate.Value - oldestDate.Value).Days + 1;
        return expectedDays > 0
            ? ((decimal)daysCovered / expectedDays) * 100m
            : 0m;
    }

    /// <summary>
    /// Calculates data coverage percentage based on recent updates (last 7 days).
    /// Used for dashboard statistics to show freshness of data.
    /// </summary>
    /// <param name="tickerSummaries">List of ticker summaries with latest dates.</param>
    /// <returns>Percentage of tickers with recent data (0-100).</returns>
    public decimal CalculateDataCoveragePercentage(List<TickerSummary> tickerSummaries)
    {
        if (tickerSummaries.Count == 0)
        {
            return 0m;
        }

        DateTime sevenDaysAgo = DateTime.Today.AddDays(-7);
        int tickersWithRecentData = tickerSummaries.Count(t =>
            t.LatestDate.HasValue && t.LatestDate.Value >= sevenDaysAgo);

        return (decimal)tickersWithRecentData / tickerSummaries.Count * 100m;
    }
}
