using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for exporting historical data and coverage reports.
/// </summary>
public class ExportHistoricalDataUseCase : IExportHistoricalDataUseCase
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly ICoverageReportExporter _coverageExporter;
    private readonly IHistoricalDataExporter _dataExporter;

    public ExportHistoricalDataUseCase(
        IHistoricalDataPort historicalDataPort,
        ICoverageReportExporter coverageExporter,
        IHistoricalDataExporter dataExporter)
    {
        _historicalDataPort = historicalDataPort;
        _coverageExporter = coverageExporter;
        _dataExporter = dataExporter;
    }

    public async Task<ExportResult> ExportCoverageReportAsync(
        TimeFrame timeFrame,
        string outputPath)
    {
        // Get all ticker summaries for the timeframe
        List<TickerSummary> summaries = await _historicalDataPort.GetAllTickerSummariesAsync(timeFrame);

        // Convert to coverage data with calculated percentages
        List<TickerCoverageData> coverageData = summaries.Select(s =>
        {
            decimal coverage = CalculateCoveragePercentage(s.OldestDate, s.LatestDate);
            int gapCount = CalculateGapCount(s.OldestDate, s.LatestDate, s.RecordCount, timeFrame);
            string status = GetStatus(coverage);

            return new TickerCoverageData(
                s.Ticker,
                s.ISIN,
                timeFrame,
                s.RecordCount,
                s.OldestDate,
                s.LatestDate,
                coverage,
                gapCount,
                status
            );
        }).ToList();

        // Export to CSV using the adapter
        return await _coverageExporter.ExportToCsvAsync(coverageData, outputPath);
    }

    public async Task<ExportResult> ExportHistoricalDataAsync(
        string ticker,
        TimeFrame timeFrame,
        ExportFormat format,
        string outputPath)
    {
        // Fetch all historical data for the ticker and timeframe
        List<HistoricalPrice> data = await _historicalDataPort.GetHistoricalDataAsync(ticker, timeFrame);

        if (data.Count == 0)
        {
            throw new InvalidOperationException($"No data found for {ticker} in timeframe {timeFrame.Unit}");
        }

        // Export using appropriate adapter based on format
        return format switch
        {
            ExportFormat.CSV => await _dataExporter.ExportToCsvAsync(data, ticker, outputPath),
            ExportFormat.JSON => await _dataExporter.ExportToJsonAsync(data, ticker, outputPath),
            _ => throw new ArgumentException($"Unsupported export format: {format}", nameof(format))
        };
    }

    private static decimal CalculateCoveragePercentage(DateTime? oldestDate, DateTime? latestDate)
    {
        if (oldestDate == null || latestDate == null)
        {
            return 0m;
        }

        int totalDays = (latestDate.Value - oldestDate.Value).Days + 1;
        if (totalDays <= 0)
        {
            return 0m;
        }

        // Simplified calculation - actual implementation would account for market days
        return 100m;
    }

    private static int CalculateGapCount(DateTime? oldestDate, DateTime? latestDate, int recordCount, TimeFrame timeFrame)
    {
        if (oldestDate == null || latestDate == null)
        {
            return 0;
        }

        // Simplified - actual implementation would detect gaps properly
        return 0;
    }

    private static string GetStatus(decimal coverage)
    {
        return coverage switch
        {
            >= 95 => "Complete",
            >= 80 => "Partial",
            _ => "Gaps"
        };
    }
}
