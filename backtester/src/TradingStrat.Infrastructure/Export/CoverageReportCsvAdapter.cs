using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;

namespace TradingStrat.Infrastructure.Export;

/// <summary>
/// Adapter for exporting coverage reports to CSV format using CsvHelper.
/// </summary>
public class CoverageReportCsvAdapter : ICoverageReportExporter
{
    public async Task<ExportResult> ExportToCsvAsync(
        List<TickerCoverageData> coverageData,
        string outputPath)
    {
        ArgumentNullException.ThrowIfNull(coverageData);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        // Ensure directory exists
        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        await using var writer = new StreamWriter(outputPath);
        await using var csv = new CsvWriter(writer, config);

        // Write header
        csv.WriteField("Ticker");
        csv.WriteField("ISIN");
        csv.WriteField("TimeFrame");
        csv.WriteField("RecordCount");
        csv.WriteField("OldestDate");
        csv.WriteField("LatestDate");
        csv.WriteField("CoveragePercentage");
        csv.WriteField("GapCount");
        csv.WriteField("Status");
        await csv.NextRecordAsync();

        // Write data rows
        foreach (TickerCoverageData data in coverageData)
        {
            csv.WriteField(data.Ticker);
            csv.WriteField(data.ISIN ?? string.Empty);
            csv.WriteField(data.TimeFrame.Unit.ToString());
            csv.WriteField(data.RecordCount);
            csv.WriteField(data.OldestDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty);
            csv.WriteField(data.LatestDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty);
            csv.WriteField(data.CoveragePercentage.ToString("F2", CultureInfo.InvariantCulture));
            csv.WriteField(data.GapCount);
            csv.WriteField(data.Status);
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync();

        // Get file size
        var fileInfo = new FileInfo(outputPath);

        return new ExportResult(
            FilePath: outputPath,
            RecordCount: coverageData.Count,
            FileSizeBytes: fileInfo.Length
        );
    }
}
