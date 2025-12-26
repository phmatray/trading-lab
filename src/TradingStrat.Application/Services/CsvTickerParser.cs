namespace TradingStrat.Application.Services;

/// <summary>
/// Service for parsing ticker symbols from CSV/text files.
/// </summary>
public interface ICsvTickerParser
{
    /// <summary>
    /// Parses ticker symbols from CSV content.
    /// Supports comma-separated, newline-separated, or semicolon-separated formats.
    /// </summary>
    /// <param name="csvContent">CSV content containing ticker symbols.</param>
    /// <returns>Result with valid and invalid tickers.</returns>
    TickerImportResult ParseTickers(string csvContent);

    /// <summary>
    /// Parses ticker symbols from a CSV file.
    /// </summary>
    /// <param name="filePath">Path to the CSV file.</param>
    /// <returns>Result with valid and invalid tickers.</returns>
    Task<TickerImportResult> ParseTickersFromFileAsync(string filePath);
}

/// <summary>
/// Implementation of CSV ticker parser.
/// </summary>
public class CsvTickerParser : ICsvTickerParser
{
    private static readonly char[] Separators = { ',', ';', '\n', '\r', '\t' };
    private const int MaxTickerLength = 10;
    private const int MinTickerLength = 1;

    public TickerImportResult ParseTickers(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return new TickerImportResult(
                ValidTickers: new List<string>(),
                InvalidTickers: new List<string>(),
                TotalLines: 0
            );
        }

        // Split by separators
        string[] parts = csvContent.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

        var validTickers = new List<string>();
        var invalidTickers = new HashSet<string>();

        foreach (string part in parts)
        {
            string ticker = CleanTicker(part);

            if (string.IsNullOrEmpty(ticker))
            {
                continue; // Skip empty entries
            }

            if (IsValidTicker(ticker))
            {
                // Deduplicate - only add if not already present
                if (!validTickers.Contains(ticker, StringComparer.OrdinalIgnoreCase))
                {
                    validTickers.Add(ticker);
                }
            }
            else
            {
                invalidTickers.Add(part.Trim());
            }
        }

        return new TickerImportResult(
            ValidTickers: validTickers,
            InvalidTickers: invalidTickers.ToList(),
            TotalLines: parts.Length
        );
    }

    public async Task<TickerImportResult> ParseTickersFromFileAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        string content = await File.ReadAllTextAsync(filePath);
        return ParseTickers(content);
    }

    private static string CleanTicker(string input)
    {
        // Remove whitespace, quotes, and other common characters
        string cleaned = input.Trim()
            .Replace("\"", "")
            .Replace("'", "")
            .Replace(" ", "")
            .ToUpperInvariant();

        return cleaned;
    }

    private static bool IsValidTicker(string ticker)
    {
        if (string.IsNullOrEmpty(ticker))
        {
            return false;
        }

        if (ticker.Length < MinTickerLength || ticker.Length > MaxTickerLength)
        {
            return false;
        }

        // Must contain only letters, numbers, dots, and hyphens
        // Examples: AAPL, BRK.B, CON3.L, 3COI.DE
        return ticker.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '-');
    }
}

/// <summary>
/// Result of ticker import/parsing operation.
/// </summary>
/// <param name="ValidTickers">List of valid, deduplicated ticker symbols.</param>
/// <param name="InvalidTickers">List of invalid entries that were rejected.</param>
/// <param name="TotalLines">Total number of entries processed.</param>
public record TickerImportResult(
    List<string> ValidTickers,
    List<string> InvalidTickers,
    int TotalLines);
