using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradyStrat.Data;
using TradyStrat.Shared.Time;

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

        db.PriceBars.AddRange(fetched);
        await db.SaveChangesAsync(ct);
        LogAppended(log, fetched.Count, ticker);
    }
}
