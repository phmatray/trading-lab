using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for exporting historical data and coverage reports.
/// Uses helper method pattern to eliminate try-catch boilerplate.
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

    public Task<Result<ExportResult>> ExportCoverageReportAsync(
        TimeFrame timeFrame,
        string outputPath)
        => ExecuteWithErrorHandling(() => ExportCoverageReportCoreAsync(timeFrame, outputPath), ErrorCodes.Data.ExportFailed);

    public Task<Result<ExportResult>> ExportHistoricalDataAsync(
        string ticker,
        TimeFrame timeFrame,
        ExportFormat format,
        string outputPath)
        => ExecuteWithErrorHandling(() => ExportHistoricalDataCoreAsync(ticker, timeFrame, format, outputPath), ErrorCodes.Data.ExportFailed);

    private static async Task<Result<T>> ExecuteWithErrorHandling<T>(
        Func<Task<Result<T>>> executeCore,
        string errorCode)
    {
        try
        {
            return await executeCore();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Result<T>.Failure(
                Error.NotFound(ex.Message, $"{errorCode}_NOT_FOUND"));
        }
        catch (ArgumentException ex)
        {
            return Result<T>.Failure(
                Error.Validation(ex.Message, $"{errorCode}_VALIDATION"));
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(
                Error.BusinessRule($"Failed: {ex.Message}", $"{errorCode}_FAILED"));
        }
    }

    private async Task<Result<ExportResult>> ExportCoverageReportCoreAsync(
        TimeFrame timeFrame,
        string outputPath)
    {
        // Get all ticker summaries for the timeframe
        List<TickerSummary> summaries = await _historicalDataPort.GetAllTickerSummariesAsync(timeFrame);

        // Convert to coverage data with calculated percentages
        List<TickerCoverageData> coverageData = summaries.Select(s =>
        {
            decimal coverage = CalculateCoveragePercentage(s.OldestDate, s.LatestDate);
            int gapCount = CalculateGapCount(s.OldestDate, s.LatestDate);
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

    private async Task<Result<ExportResult>> ExportHistoricalDataCoreAsync(
        string ticker,
        TimeFrame timeFrame,
        ExportFormat format,
        string outputPath)
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

    private static decimal CalculateCoveragePercentage(DateTime? oldestDate, DateTime? latestDate)
    {
        if (oldestDate is null || latestDate is null)
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

    private static int CalculateGapCount(DateTime? oldestDate, DateTime? latestDate)
    {
        if (oldestDate is null || latestDate is null)
        {
            return 0;
        }

        // TODO: Implement actual gap detection logic
        // This should analyze the date range and detect missing market days
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
