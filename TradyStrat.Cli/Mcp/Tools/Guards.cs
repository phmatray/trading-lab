using System.Globalization;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;

namespace TradyStrat.Cli.Mcp.Tools;

internal sealed class Guards(ListInstrumentsUseCase listInstruments)
{
    private IReadOnlyList<Instrument>? _cached;

    public async Task<Instrument> ResolveInstrumentOrThrow(string ticker, CancellationToken ct)
    {
        _cached ??= await listInstruments.ExecuteAsync(Unit.Value, ct);
        var match = _cached.FirstOrDefault(i => i.Ticker == ticker);
        if (match is null)
        {
            var known = string.Join(", ", _cached.Select(i => i.Ticker));
            throw new ArgumentException(
                $"Unknown instrument '{ticker}'. Known tickers: {known}. Call list_instruments to see valid tickers.");
        }

        return match;
    }

    public static (DateOnly from, DateOnly to) ResolveDateRange(
        string? from, string? to, int defaultBack, DateOnly clockToday)
    {
        var resolvedTo = ParseDate(to, "to") ?? clockToday;
        var resolvedFrom = ParseDate(from, "from") ?? resolvedTo.AddDays(-defaultBack);
        if (resolvedFrom > resolvedTo)
            throw new ArgumentException(
                $"from ({resolvedFrom:yyyy-MM-dd}) must be on or before to ({resolvedTo:yyyy-MM-dd}).");
        return (resolvedFrom, resolvedTo);
    }

    private static DateOnly? ParseDate(string? s, string field)
    {
        if (s is null) return null;
        if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        throw new ArgumentException($"Invalid date '{s}' for {field} — use ISO YYYY-MM-DD.");
    }
}
