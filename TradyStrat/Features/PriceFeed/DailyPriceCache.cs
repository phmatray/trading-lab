using Microsoft.EntityFrameworkCore;
using TradyStrat.Features.PriceFeed.Providers;
using TradyStrat.Data;
using TradyStrat.Common.Time;

namespace TradyStrat.Features.PriceFeed;

public sealed partial class DailyPriceCache(
    IPriceFeed feed,
    AppDbContext db,
    IClock clock,
    ILogger<DailyPriceCache> log)
{
    [LoggerMessage(Level = LogLevel.Information, Message = "PriceFeed: no new bars for {Ticker}")]
    private static partial void LogNoBars(ILogger logger, string ticker);

    [LoggerMessage(Level = LogLevel.Information, Message = "PriceFeed: appended {N} bars for {Ticker}")]
    private static partial void LogAppended(ILogger logger, int n, string ticker);

    public async Task EnsureFreshAsync(string ticker, CancellationToken ct)
    {
        var today  = clock.TodayInExchangeTzFor(ticker);
        var latest = await db.PriceBars
            .Where(b => b.Ticker == ticker)
            .OrderByDescending(b => b.Date)
            .Select(b => (DateOnly?)b.Date)
            .FirstOrDefaultAsync(ct);

        if (latest == today) return;

        var from = latest?.AddDays(1) ?? today.AddYears(-2);
        var fetched = await feed.FetchDailyAsync(ticker, from, today, ct);
        if (fetched.Count == 0)
        {
            LogNoBars(log, ticker);
            return;
        }

        // Dedupe against rows already in the DB. Yahoo's day boundary doesn't
        // align with the exchange's local date and the period query can return
        // bars we already have (sometimes with UTC dates that fall before our
        // local `from`). Filter against the actual fetched dates rather than a
        // local-tz date range.
        var fetchedDates = fetched.Select(b => b.Date).ToList();
        var existingDates = await db.PriceBars
            .Where(b => b.Ticker == ticker && fetchedDates.Contains(b.Date))
            .Select(b => b.Date)
            .ToListAsync(ct);
        var existingSet = existingDates.ToHashSet();
        var newBars = fetched.Where(b => !existingSet.Contains(b.Date)).ToList();
        if (newBars.Count == 0)
        {
            LogNoBars(log, ticker);
            return;
        }

        db.PriceBars.AddRange(newBars);
        await db.SaveChangesAsync(ct);
        LogAppended(log, newBars.Count, ticker);
    }
}
