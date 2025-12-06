using System.Globalization;
using System.Text.Json;
using CsvHelper;
using TradingStrat.Application.Ports.Outbound;

namespace TradingStrat.Infrastructure.Export;

public class ExportAdapter : IExportPort
{
    public async Task ExportToCsvAsync<T>(IEnumerable<T> data, string filePath)
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

    public async Task ExportToJsonAsync<T>(T data, string filePath)
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
}
