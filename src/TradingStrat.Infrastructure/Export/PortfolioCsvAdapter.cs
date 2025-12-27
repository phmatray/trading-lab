using System.Text;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Infrastructure.Export;

/// <summary>
/// Adapter for importing and exporting portfolio positions via CSV files.
/// </summary>
public class PortfolioCsvAdapter : IPortfolioExportPort
{
    private readonly IPortfolioPort _portfolioPort;

    public PortfolioCsvAdapter(IPortfolioPort portfolioPort)
    {
        _portfolioPort = portfolioPort ?? throw new ArgumentNullException(nameof(portfolioPort));
    }

    /// <inheritdoc />
    public async Task ExportPortfolioToCsvAsync(int portfolioId, string filePath)
    {
        List<Position> positions = await _portfolioPort.GetPositionsByPortfolioAsync(portfolioId);

        var csv = new StringBuilder();
        csv.AppendLine("Ticker,Quantity,EntryPrice,EntryDate,Notes");

        foreach (Position position in positions)
        {
            // Escape notes field if it contains commas or quotes
            string escapedNotes = EscapeCsvField(position.Notes ?? string.Empty);

            csv.AppendLine(
                $"{position.Ticker}," +
                $"{position.Quantity}," +
                $"{position.EntryPrice}," +
                $"{position.EntryDate:yyyy-MM-dd}," +
                $"{escapedNotes}");
        }

        await File.WriteAllTextAsync(filePath, csv.ToString());
    }

    /// <inheritdoc />
    public async Task<List<Position>> ImportPositionsFromCsvAsync(int portfolioId, string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV file not found: {filePath}");
        }

        string[] lines = await File.ReadAllLinesAsync(filePath);
        if (lines.Length < 2)
        {
            throw new InvalidOperationException("CSV file is empty or contains only header");
        }

        List<Position> positions = new();

        // Skip header (first line)
        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                string[] fields = ParseCsvLine(lines[i]);

                if (fields.Length < 4)
                {
                    throw new InvalidOperationException(
                        $"Line {i + 1}: Invalid CSV format. Expected at least 4 fields (Ticker, Quantity, EntryPrice, EntryDate)");
                }

                var position = new Position
                {
                    PortfolioId = portfolioId,
                    Ticker = fields[0].Trim().ToUpperInvariant(),
                    Quantity = int.Parse(fields[1].Trim()),
                    EntryPrice = decimal.Parse(fields[2].Trim()),
                    EntryDate = DateTime.Parse(fields[3].Trim()),
                    Notes = fields.Length > 4 ? fields[4].Trim() : null
                };

                // Validate parsed data
                if (string.IsNullOrWhiteSpace(position.Ticker))
                {
                    throw new InvalidOperationException($"Line {i + 1}: Ticker cannot be empty");
                }

                if (position.Quantity <= 0)
                {
                    throw new InvalidOperationException($"Line {i + 1}: Quantity must be positive");
                }

                if (position.EntryPrice <= 0)
                {
                    throw new InvalidOperationException($"Line {i + 1}: Entry price must be positive");
                }

                Position createdPosition = await _portfolioPort.AddPositionAsync(position);
                positions.Add(createdPosition);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    $"Line {i + 1}: Failed to parse CSV data. {ex.Message}", ex);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"Line {i + 1}: Error processing position. {ex.Message}", ex);
            }
        }

        return positions;
    }

    /// <summary>
    /// Parses a CSV line handling quoted fields and escaped quotes.
    /// </summary>
    /// <param name="line">The CSV line to parse.</param>
    /// <returns>Array of field values.</returns>
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // Handle escaped quotes ("")
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        // Add last field
        fields.Add(currentField.ToString());

        return fields.ToArray();
    }

    /// <summary>
    /// Escapes a CSV field by wrapping in quotes if it contains special characters.
    /// </summary>
    /// <param name="field">The field to escape.</param>
    /// <returns>Escaped field value.</returns>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        // Check if field needs quoting (contains comma, quote, or newline)
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            // Escape quotes by doubling them
            string escaped = field.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        return field;
    }
}
