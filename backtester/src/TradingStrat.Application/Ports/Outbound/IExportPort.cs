using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Port for exporting coverage reports to CSV format.
/// </summary>
public interface ICoverageReportExporter
{
    /// <summary>
    /// Exports coverage data to CSV file.
    /// </summary>
    /// <param name="coverageData">List of ticker coverage information.</param>
    /// <param name="outputPath">Full path to save the CSV file.</param>
    /// <returns>Result with file path and metadata.</returns>
    Task<ExportResult> ExportToCsvAsync(
        List<TickerCoverageData> coverageData,
        string outputPath);
}

/// <summary>
/// Port for exporting historical data to various formats.
/// </summary>
public interface IHistoricalDataExporter
{
    /// <summary>
    /// Exports historical price data to CSV format.
    /// </summary>
    Task<ExportResult> ExportToCsvAsync(
        List<HistoricalPrice> data,
        string ticker,
        string outputPath);

    /// <summary>
    /// Exports historical price data to JSON format.
    /// </summary>
    Task<ExportResult> ExportToJsonAsync(
        List<HistoricalPrice> data,
        string ticker,
        string outputPath);
}

/// <summary>
/// Data structure for ticker coverage information.
/// </summary>
public record TickerCoverageData(
    string Ticker,
    string? ISIN,
    TimeFrame TimeFrame,
    int RecordCount,
    DateTime? OldestDate,
    DateTime? LatestDate,
    decimal CoveragePercentage,
    int GapCount,
    string Status);
