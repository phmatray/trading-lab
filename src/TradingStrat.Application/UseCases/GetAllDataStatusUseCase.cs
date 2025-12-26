using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for retrieving comprehensive data status for all tickers.
/// </summary>
public class GetAllDataStatusUseCase : IGetAllDataStatusUseCase
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private static readonly TimeFrame D1_TIMEFRAME = new() { Unit = TimeFrameUnit.D1 };

    public GetAllDataStatusUseCase(IHistoricalDataPort historicalDataPort)
    {
        _historicalDataPort = historicalDataPort;
    }

    public async Task<AllDataStatusResult> ExecuteAsync()
    {
        // Get all unique tickers
        List<string> tickers = await _historicalDataPort.GetAllTickersAsync();

        if (!tickers.Any())
        {
            return new AllDataStatusResult(
                TotalTickers: 0,
                TotalRecords: 0,
                AverageCoveragePercentage: 0m,
                TickerStatuses: new List<TickerDataStatus>()
            );
        }

        // Get status for each ticker in parallel
        var statusTasks = tickers.Select(async ticker =>
        {
            return await GetTickerStatusAsync(ticker);
        });

        List<TickerDataStatus> tickerStatuses = (await Task.WhenAll(statusTasks)).ToList();

        int totalRecords = tickerStatuses.Sum(s => s.RecordCount);
        decimal avgCoverage = tickerStatuses.Any()
            ? tickerStatuses.Average(s => s.CoveragePercentage)
            : 0m;

        return new AllDataStatusResult(
            TotalTickers: tickers.Count,
            TotalRecords: totalRecords,
            AverageCoveragePercentage: avgCoverage,
            TickerStatuses: tickerStatuses.OrderBy(s => s.Ticker).ToList()
        );
    }

    private async Task<TickerDataStatus> GetTickerStatusAsync(string ticker)
    {
        // Get data summary for daily timeframe
        DataSummaryResult summary = await _historicalDataPort.GetDataSummaryAsync(ticker, D1_TIMEFRAME);

        // Get all historical prices to detect gaps
        List<HistoricalPrice> prices = await _historicalDataPort.GetHistoricalDataAsync(ticker, D1_TIMEFRAME);

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
