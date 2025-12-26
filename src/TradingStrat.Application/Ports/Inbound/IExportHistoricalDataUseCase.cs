using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for exporting historical data and coverage reports to various formats.
/// </summary>
public interface IExportHistoricalDataUseCase
{
    /// <summary>
    /// Exports a coverage report for all tickers in a specific timeframe.
    /// </summary>
    /// <param name="timeFrame">Timeframe to generate report for.</param>
    /// <param name="outputPath">Full path where the report file should be saved.</param>
    /// <returns>Result containing file path and metadata.</returns>
    Task<ExportResult> ExportCoverageReportAsync(
        TimeFrame timeFrame,
        string outputPath);

    /// <summary>
    /// Exports historical data for a specific ticker to a file.
    /// </summary>
    /// <param name="ticker">Ticker symbol to export.</param>
    /// <param name="timeFrame">Timeframe of the data.</param>
    /// <param name="format">Export format (CSV or JSON).</param>
    /// <param name="outputPath">Full path where the file should be saved.</param>
    /// <returns>Result containing file path and metadata.</returns>
    Task<ExportResult> ExportHistoricalDataAsync(
        string ticker,
        TimeFrame timeFrame,
        ExportFormat format,
        string outputPath);
}

/// <summary>
/// Export format options.
/// </summary>
public enum ExportFormat
{
    CSV,
    JSON
}

/// <summary>
/// Result of an export operation.
/// </summary>
/// <param name="FilePath">Full path to the exported file.</param>
/// <param name="RecordCount">Number of records exported.</param>
/// <param name="FileSizeBytes">Size of the exported file in bytes.</param>
public record ExportResult(
    string FilePath,
    int RecordCount,
    long FileSizeBytes);
