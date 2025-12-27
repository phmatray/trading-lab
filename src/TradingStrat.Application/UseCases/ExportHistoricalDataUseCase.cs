using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
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

    public async Task<Result<ExportResult>> ExportCoverageReportAsync(
        TimeFrame timeFrame,
        string outputPath)
    {
        try
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
            ExportResult result = await _coverageExporter.ExportToCsvAsync(coverageData, outputPath);

            return Result<ExportResult>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<ExportResult>.Failure(
                Error.BusinessRule($"Failed to export coverage report: {ex.Message}", "EXPORT_COVERAGE_FAILED"));
        }
    }

    public async Task<Result<ExportResult>> ExportHistoricalDataAsync(
        string ticker,
        TimeFrame timeFrame,
        ExportFormat format,
        string outputPath)
    {
        try
        {
            // Fetch all historical data for the ticker and timeframe
            List<HistoricalPrice> data = await _historicalDataPort.GetHistoricalDataAsync(ticker, timeFrame);

            if (data.Count == 0)
            {
                return Result<ExportResult>.Failure(
                    Error.NotFound($"No data found for {ticker} in timeframe {timeFrame.Unit}", "NO_DATA_TO_EXPORT"));
            }

            // Export using appropriate adapter based on format
            ExportResult result = format switch
            {
                ExportFormat.CSV => await _dataExporter.ExportToCsvAsync(data, ticker, outputPath),
                ExportFormat.JSON => await _dataExporter.ExportToJsonAsync(data, ticker, outputPath),
                _ => throw new ArgumentException($"Unsupported export format: {format}", nameof(format))
            };

            return Result<ExportResult>.Success(result);
        }
        catch (ArgumentException ex)
        {
            return Result<ExportResult>.Failure(
                Error.Validation(ex.Message, "UNSUPPORTED_EXPORT_FORMAT"));
        }
        catch (Exception ex)
        {
            return Result<ExportResult>.Failure(
                Error.BusinessRule($"Failed to export data for {ticker}: {ex.Message}", "EXPORT_DATA_FAILED"));
        }
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
