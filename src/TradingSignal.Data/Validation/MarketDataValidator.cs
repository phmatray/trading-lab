using Microsoft.Extensions.Logging;
using TradingSignal.Core;

namespace TradingSignal.Data.Validation;

public static partial class MarketDataValidator
{
    // Throws on hard violations (non-monotonic, duplicate timestamps).
    // Logs a warning on soft violations (gaps > 1 interval).
    public static void Validate(IReadOnlyList<Candle> candles, TimeSpan interval, ILogger? logger)
    {
        if (candles.Count < 2) return;

        var maxGap = TimeSpan.FromTicks(interval.Ticks * 2);
        for (var i = 1; i < candles.Count; i++)
        {
            var prev = candles[i - 1].OpenTimeUtc;
            var curr = candles[i].OpenTimeUtc;

            if (curr == prev)
                throw new InvalidOperationException($"Duplicate candle timestamp at index {i}: {curr:o}");
            if (curr < prev)
                throw new InvalidOperationException($"Non-monotonic candle stream at index {i}: {curr:o} after {prev:o}");

            var delta = curr - prev;
            if (delta > maxGap && logger is not null)
            {
                LogGapDetected(logger, prev, curr, delta, interval);
            }
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Gap detected between {Prev} and {Curr}: {Delta} (interval {Interval})")]
    private static partial void LogGapDetected(ILogger logger, DateTime prev, DateTime curr, TimeSpan delta, TimeSpan interval);
}
