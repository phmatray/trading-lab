using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CsvHelper;
using CsvHelper.Configuration;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Infrastructure.Export;

/// <summary>
/// Adapter for exporting historical data to CSV and JSON formats.
/// </summary>
public class HistoricalDataExportAdapter : IHistoricalDataExporter
{
    public async Task<ExportResult> ExportToCsvAsync(
        List<HistoricalPrice> data,
        string ticker,
        string outputPath)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);
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
        csv.WriteField("DateTime");
        csv.WriteField("Open");
        csv.WriteField("High");
        csv.WriteField("Low");
        csv.WriteField("Close");
        csv.WriteField("Volume");
        await csv.NextRecordAsync();

        // Write data rows (sorted by date)
        foreach (HistoricalPrice price in data.OrderBy(p => p.DateTime))
        {
            csv.WriteField(price.Ticker);
            csv.WriteField(price.ISIN ?? string.Empty);
            csv.WriteField(price.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            csv.WriteField(price.Open?.ToString("F6") ?? string.Empty);
            csv.WriteField(price.High?.ToString("F6") ?? string.Empty);
            csv.WriteField(price.Low?.ToString("F6") ?? string.Empty);
            csv.WriteField(price.Close?.ToString("F6") ?? string.Empty);
            csv.WriteField(price.Volume?.ToString() ?? string.Empty);
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync();

        // Get file size
        var fileInfo = new FileInfo(outputPath);

        return new ExportResult(
            FilePath: outputPath,
            RecordCount: data.Count,
            FileSizeBytes: fileInfo.Length
        );
    }

    public async Task<ExportResult> ExportToJsonAsync(
        List<HistoricalPrice> data,
        string ticker,
        string outputPath)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        // Ensure directory exists
        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create export model
        var exportData = new HistoricalDataExport
        {
            Ticker = ticker,
            ISIN = data.FirstOrDefault()?.ISIN,
            RecordCount = data.Count,
            OldestDate = data.Min(p => p.DateTime),
            LatestDate = data.Max(p => p.DateTime),
            ExportedAt = DateTime.UtcNow,
            Data = data.OrderBy(p => p.DateTime).Select(p => new PriceDataPoint
            {
                DateTime = p.DateTime,
                Open = p.Open,
                High = p.High,
                Low = p.Low,
                Close = p.Close,
                Volume = p.Volume
            }).ToList()
        };

        // Serialize with pretty printing
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        await using (var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
        {
            await JsonSerializer.SerializeAsync(stream, exportData, options);
            await stream.FlushAsync();
        } // Stream is disposed here

        // Get file size after stream is closed
        var fileInfo = new FileInfo(outputPath);

        return new ExportResult(
            FilePath: outputPath,
            RecordCount: data.Count,
            FileSizeBytes: fileInfo.Length
        );
    }
}

/// <summary>
/// Root object for JSON export.
/// </summary>
internal class HistoricalDataExport
{
    public string Ticker { get; set; } = string.Empty;
    public string? ISIN { get; set; }
    public int RecordCount { get; set; }
    public DateTime OldestDate { get; set; }
    public DateTime LatestDate { get; set; }
    public DateTime ExportedAt { get; set; }
    public List<PriceDataPoint> Data { get; set; } = new();
}

/// <summary>
/// Individual price data point for JSON export.
/// </summary>
internal class PriceDataPoint
{
    public DateTime DateTime { get; set; }
    public decimal? Open { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal? Close { get; set; }
    public long? Volume { get; set; }
}
