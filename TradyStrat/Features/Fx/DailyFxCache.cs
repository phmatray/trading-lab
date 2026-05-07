using Microsoft.EntityFrameworkCore;
using TradyStrat.Features.Fx.Providers;
using TradyStrat.Data;
using TradyStrat.Common.Time;

namespace TradyStrat.Features.Fx;

public sealed partial class DailyFxCache(
    IFxRateProvider provider,
    AppDbContext db,
    IClock clock,
    ILogger<DailyFxCache> log)
{
    public async Task EnsureFreshAsync(string pair, CancellationToken ct)
    {
        var today  = clock.TodayInExchangeTzFor(pair);
        var latest = await db.FxRates
            .Where(r => r.Pair == pair)
            .OrderByDescending(r => r.Date)
            .Select(r => (DateOnly?)r.Date)
            .FirstOrDefaultAsync(ct);

        if (latest == today) return;

        var from = latest?.AddDays(1) ?? today.AddYears(-2);
        var rates = await provider.FetchAsync(pair, from, today, ct);
        if (rates.Count == 0)
        {
            LogNoRates(log, pair);
            return;
        }

        var fetchedDates = rates.Select(r => r.Date).ToList();
        var existingDates = await db.FxRates
            .Where(r => r.Pair == pair && fetchedDates.Contains(r.Date))
            .Select(r => r.Date)
            .ToListAsync(ct);
        var existingSet = existingDates.ToHashSet();
        var newRates = rates.Where(r => !existingSet.Contains(r.Date)).ToList();
        if (newRates.Count == 0)
        {
            LogNoRates(log, pair);
            return;
        }

        db.FxRates.AddRange(newRates);
        await db.SaveChangesAsync(ct);
        LogAppended(log, newRates.Count, pair);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fx: no new rates for {Pair}")]
    private static partial void LogNoRates(ILogger logger, string pair);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fx: appended {N} rates for {Pair}")]
    private static partial void LogAppended(ILogger logger, int n, string pair);
}
