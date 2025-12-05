using System.Globalization;
using System.Text.Json;
using CsvHelper;
using TradingStrat.Models;

namespace TradingStrat.Services;

public class ExportService : IExportService
{
    public async Task ExportToCsvAsync(List<HistoricalPrice> data, string filePath)
    {
        try
        {
            await using var writer = new StreamWriter(filePath);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            await csv.WriteRecordsAsync(data);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to export to CSV: {ex.Message}", ex);
        }
    }

    public async Task ExportToJsonAsync(List<HistoricalPrice> data, string filePath)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to export to JSON: {ex.Message}", ex);
        }
    }

    public async Task ExportBacktestResultAsync(BacktestResult result, string filePath)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(result, options);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to export backtest result: {ex.Message}", ex);
        }
    }
}
