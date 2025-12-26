using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for retrieving comprehensive data status for all tickers.
/// Supports filtering, sorting, and pagination.
/// </summary>
public class GetAllDataStatusUseCase : IGetAllDataStatusUseCase
{
    private readonly IHistoricalDataPort _historicalDataPort;

    public GetAllDataStatusUseCase(IHistoricalDataPort historicalDataPort)
    {
        _historicalDataPort = historicalDataPort;
    }

    public async Task<AllDataStatusResult> ExecuteAsync(DataStatusQuery? query = null)
    {
        // Use default query if not provided
        query ??= new DataStatusQuery();

        // Determine timeframe (default to D1)
        TimeFrame timeFrame = query.TimeFrame ?? new TimeFrame { Unit = TimeFrameUnit.D1 };

        // Get all ticker summaries for the specified timeframe (efficient single query)
        List<TickerSummary> summaries = await _historicalDataPort.GetAllTickerSummariesAsync(timeFrame);

        if (!summaries.Any())
        {
            return new AllDataStatusResult(
                TotalTickers: 0,
                TotalRecords: 0,
                AverageCoveragePercentage: 0m,
                TickerStatuses: new List<TickerDataStatus>(),
                TotalPages: 0,
                CurrentPage: 1,
                PageSize: query.PageSize
            );
        }

        // Convert summaries to full status objects (with gap detection)
        var statusTasks = summaries.Select(async summary =>
        {
            return await GetTickerStatusAsync(summary.Ticker, timeFrame);
        });

        List<TickerDataStatus> allStatuses = (await Task.WhenAll(statusTasks)).ToList();

        // Apply filters
        IEnumerable<TickerDataStatus> filteredStatuses = ApplyFilters(allStatuses, query);

        // Calculate totals before pagination
        int totalTickers = filteredStatuses.Count();
        int totalRecords = filteredStatuses.Sum(s => s.RecordCount);
        decimal avgCoverage = filteredStatuses.Any()
            ? filteredStatuses.Average(s => s.CoveragePercentage)
            : 0m;

        // Apply sorting
        IEnumerable<TickerDataStatus> sortedStatuses = ApplySorting(filteredStatuses, query);

        // Apply pagination
        int totalPages = (int)Math.Ceiling((double)totalTickers / query.PageSize);
        int skip = (query.PageNumber - 1) * query.PageSize;
        List<TickerDataStatus> pagedStatuses = sortedStatuses
            .Skip(skip)
            .Take(query.PageSize)
            .ToList();

        return new AllDataStatusResult(
            TotalTickers: totalTickers,
            TotalRecords: totalRecords,
            AverageCoveragePercentage: avgCoverage,
            TickerStatuses: pagedStatuses,
            TotalPages: totalPages,
            CurrentPage: query.PageNumber,
            PageSize: query.PageSize
        );
    }

    private IEnumerable<TickerDataStatus> ApplyFilters(
        IEnumerable<TickerDataStatus> statuses,
        DataStatusQuery query)
    {
        // Filter by search term
        if (!string.IsNullOrWhiteSpace(query.SearchTicker))
        {
            string searchTerm = query.SearchTicker.ToUpperInvariant();
            statuses = statuses.Where(s => s.Ticker.ToUpperInvariant().Contains(searchTerm));
        }

        // Filter by status
        if (query.StatusFilter.HasValue && query.StatusFilter != DataStatusFilter.All)
        {
            statuses = query.StatusFilter.Value switch
            {
                DataStatusFilter.Complete => statuses.Where(s => s.CoveragePercentage >= 95m),
                DataStatusFilter.Partial => statuses.Where(s => s.CoveragePercentage >= 80m && s.CoveragePercentage < 95m),
                DataStatusFilter.WithGaps => statuses.Where(s => s.CoveragePercentage < 80m),
                _ => statuses
            };
        }

        // Filter by coverage range
        if (query.MinCoverage.HasValue)
        {
            statuses = statuses.Where(s => s.CoveragePercentage >= query.MinCoverage.Value);
        }

        if (query.MaxCoverage.HasValue)
        {
            statuses = statuses.Where(s => s.CoveragePercentage <= query.MaxCoverage.Value);
        }

        return statuses;
    }

    private IEnumerable<TickerDataStatus> ApplySorting(
        IEnumerable<TickerDataStatus> statuses,
        DataStatusQuery query)
    {
        IOrderedEnumerable<TickerDataStatus> orderedStatuses = query.SortBy switch
        {
            SortColumn.Ticker => query.SortDirection == SortDirection.Ascending
                ? statuses.OrderBy(s => s.Ticker)
                : statuses.OrderByDescending(s => s.Ticker),
            SortColumn.RecordCount => query.SortDirection == SortDirection.Ascending
                ? statuses.OrderBy(s => s.RecordCount)
                : statuses.OrderByDescending(s => s.RecordCount),
            SortColumn.Coverage => query.SortDirection == SortDirection.Ascending
                ? statuses.OrderBy(s => s.CoveragePercentage)
                : statuses.OrderByDescending(s => s.CoveragePercentage),
            SortColumn.OldestDate => query.SortDirection == SortDirection.Ascending
                ? statuses.OrderBy(s => s.OldestDate)
                : statuses.OrderByDescending(s => s.OldestDate),
            SortColumn.LatestDate => query.SortDirection == SortDirection.Ascending
                ? statuses.OrderBy(s => s.LatestDate)
                : statuses.OrderByDescending(s => s.LatestDate),
            _ => statuses.OrderBy(s => s.Ticker)
        };

        return orderedStatuses;
    }

    private async Task<TickerDataStatus> GetTickerStatusAsync(string ticker, TimeFrame timeFrame)
    {
        // Get data summary
        DataSummaryResult summary = await _historicalDataPort.GetDataSummaryAsync(ticker, timeFrame);

        // Get all historical prices to detect gaps
        List<HistoricalPrice> prices = await _historicalDataPort.GetHistoricalDataAsync(ticker, timeFrame);

        List<DateGap> gaps = DetectGaps(prices);

        int daysCovered = prices.Count;
        int expectedDays = summary.OldestDate != null && summary.LatestDate != null
            ? (summary.LatestDate.Value - summary.OldestDate.Value).Days + 1
            : 0;

        decimal coveragePercentage = expectedDays > 0
            ? ((decimal)daysCovered / expectedDays) * 100m
            : 0m;

        return new TickerDataStatus(
            Ticker: ticker,
            ISIN: summary.ISIN,
            RecordCount: summary.TotalRecords,
            OldestDate: summary.OldestDate,
            LatestDate: summary.LatestDate,
            DaysCovered: daysCovered,
            CoveragePercentage: coveragePercentage,
            Gaps: gaps
        );
    }

    private List<DateGap> DetectGaps(List<HistoricalPrice> prices)
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
}
