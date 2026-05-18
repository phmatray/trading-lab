using System.Globalization;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Application.Trades;

public sealed record CsvTradeRow(
    DateOnly ExecutedOn, TradeSide Side,
    decimal Quantity, decimal PricePerShare, decimal FeesEur, string? Note);

public static class CsvImportService
{
    private static readonly string[] Required = ["date", "side", "qty", "price", "fees"];

    public static IReadOnlyList<CsvTradeRow> Parse(TextReader reader)
    {
        var headerLine = reader.ReadLine()
            ?? throw new CsvImportException("CSV is empty.");

        var headers = headerLine.Split(',').Select(h => h.Trim().ToLowerInvariant()).ToArray();
        var idx = new Dictionary<string, int>();
        foreach (var name in Required)
        {
            var i = Array.IndexOf(headers, name);
            if (i < 0) throw new CsvImportException($"Missing required column '{name}'.");
            idx[name] = i;
        }

        var rows = new List<CsvTradeRow>();
        var line = 1;
        string? raw;
        while ((raw = reader.ReadLine()) is not null)
        {
            line++;
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var cells = raw.Split(',').Select(c => c.Trim()).ToArray();
            try
            {
                var date  = DateOnly.Parse(cells[idx["date"]],  CultureInfo.InvariantCulture);
                var side  = ParseSide(cells[idx["side"]], line);
                var qty   = decimal.Parse(cells[idx["qty"]],    CultureInfo.InvariantCulture);
                var price = decimal.Parse(cells[idx["price"]],  CultureInfo.InvariantCulture);
                var fees  = decimal.Parse(cells[idx["fees"]],   CultureInfo.InvariantCulture);
                rows.Add(new CsvTradeRow(date, side, qty, price, fees, Note: null));
            }
            catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException)
            {
                throw new CsvImportException(ex.Message, line);
            }
        }
        return rows;
    }

    private static TradeSide ParseSide(string raw, int line) => raw.ToLowerInvariant() switch
    {
        "buy"  => TradeSide.Buy,
        "sell" => TradeSide.Sell,
        _      => throw new CsvImportException($"Unknown side '{raw}'.", line)
    };
}
