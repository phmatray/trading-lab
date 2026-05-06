using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradyStrat.Data;
using TradyStrat.Shared.Time;

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

        db.FxRates.AddRange(rates);
        await db.SaveChangesAsync(ct);
        LogAppended(log, rates.Count, pair);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fx: no new rates for {Pair}")]
    private static partial void LogNoRates(ILogger logger, string pair);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fx: appended {N} rates for {Pair}")]
    private static partial void LogAppended(ILogger logger, int n, string pair);
}
