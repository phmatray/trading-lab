namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Outbound port for exporting data to various file formats.
/// Supports CSV (using CsvHelper) and JSON (using System.Text.Json).
/// Replaces IExportService from the original architecture.
/// </summary>
public interface IExportPort
{
    /// <summary>
    /// Exports a collection of objects to a CSV file.
    /// Uses CsvHelper for serialization with default configuration.
    /// </summary>
    /// <typeparam name="T">Type of objects to export (must have public properties).</typeparam>
    /// <param name="data">Collection of objects to serialize to CSV.</param>
    /// <param name="filePath">Full path to the output CSV file.</param>
    Task ExportToCsvAsync<T>(IEnumerable<T> data, string filePath);

    /// <summary>
    /// Exports an object to a JSON file with pretty-printing.
    /// Uses System.Text.Json with indented formatting.
    /// </summary>
    /// <typeparam name="T">Type of object to export.</typeparam>
    /// <param name="data">Object to serialize to JSON.</param>
    /// <param name="filePath">Full path to the output JSON file.</param>
    Task ExportToJsonAsync<T>(T data, string filePath);
}
