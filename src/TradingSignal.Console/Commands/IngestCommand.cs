using Microsoft.Extensions.Logging;
using TradingSignal.ConsoleApp.Configuration;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;

namespace TradingSignal.ConsoleApp.Commands;

public sealed partial class IngestCommand(
    IMarketDataSource marketData,
    AppConfig config,
    ILogger<IngestCommand> logger)
{
    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        TimeSpan interval = IntervalParser.Parse(config.Market.Interval);
        DateTime endUtc = DateTime.UtcNow.Date.AddDays(-1);
        DateTime startUtc = endUtc.AddDays(-config.Market.HistoryDays);

        LogIngesting(logger, config.Market.Symbol, config.Market.Interval, startUtc, endUtc);

        IReadOnlyList<Candle> candles = await marketData.GetCandlesAsync(
            config.Market.Symbol, interval, startUtc, endUtc, ct).ConfigureAwait(false);

        LogIngested(logger, candles.Count);
        if (candles.Count > 0)
        {
            LogRangeOnDisk(logger, candles[0].OpenTimeUtc, candles[^1].OpenTimeUtc);
        }
        return 0;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Ingesting {Symbol} {Interval} from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}")]
    private static partial void LogIngesting(ILogger logger, string symbol, string interval, DateTime start, DateTime end);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Ingested {Count} candles")]
    private static partial void LogIngested(ILogger logger, int count);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Range on disk: {First} .. {Last}")]
    private static partial void LogRangeOnDisk(ILogger logger, DateTime first, DateTime last);
}
