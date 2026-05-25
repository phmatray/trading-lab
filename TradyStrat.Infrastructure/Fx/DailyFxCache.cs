using Microsoft.EntityFrameworkCore;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Application.Fx.Providers;

namespace TradyStrat.Infrastructure.Fx;

public sealed partial class DailyFxCache(
    IFxRateProvider provider,
    AppDbContext db,
    IClock clock,
    ILogger<DailyFxCache> log)
{
    public async Task EnsureFreshAsync(string @base, string quote, CancellationToken ct)
    {
        // FX trades 24/5 in UTC; the existing pair-keyed timezone fallback to UTC is fine.
        var today = clock.TodayInExchangeTzFor($"{@base}{quote}");
        var b = Currency.Parse(@base);
        var q = Currency.Parse(quote);
        var latest = await db.FxRates
            .Where(r => r.Pair.Base == b && r.Pair.Quote == q)
            .OrderByDescending(r => r.Date)
            .Select(r => (DateOnly?)r.Date)
            .FirstOrDefaultAsync(ct);

        if (latest == today) return;

        var from = latest?.AddDays(1) ?? today.AddYears(-2);
        var rates = await provider.FetchAsync(@base, quote, from, today, ct);
        if (rates.Count == 0)
        {
            LogNoRates(log, @base, quote);
            return;
        }

        var fetchedDates = rates.Select(r => r.Date).ToList();
        var existingDates = await db.FxRates
            .Where(r => r.Pair.Base == b && r.Pair.Quote == q && fetchedDates.Contains(r.Date))
            .Select(r => r.Date)
            .ToListAsync(ct);
        var existingSet = existingDates.ToHashSet();
        var newRates = rates.Where(r => !existingSet.Contains(r.Date)).ToList();
        if (newRates.Count == 0)
        {
            LogNoRates(log, @base, quote);
            return;
        }

        db.FxRates.AddRange(newRates);
        await db.SaveChangesAsync(ct);
        LogAppended(log, newRates.Count, @base, quote);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fx: no new rates for {Base}/{Quote}")]
    private static partial void LogNoRates(ILogger logger, string @base, string quote);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fx: appended {N} rates for {Base}/{Quote}")]
    private static partial void LogAppended(ILogger logger, int n, string @base, string quote);
}
