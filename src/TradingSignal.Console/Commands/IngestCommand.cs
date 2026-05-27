using Microsoft.Extensions.Logging;
using TradingSignal.ConsoleApp.Configuration;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;

namespace TradingSignal.ConsoleApp.Commands;

public sealed class IngestCommand(
    IMarketDataSource marketData,
    AppConfig config,
    ILogger<IngestCommand> logger)
{
    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        TimeSpan interval = IntervalParser.Parse(config.Market.Interval);
        DateTime endUtc = DateTime.UtcNow.Date.AddDays(-1);
        DateTime startUtc = endUtc.AddDays(-config.Market.HistoryDays);

        logger.LogInformation(
            "Ingesting {Symbol} {Interval} from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}",
            config.Market.Symbol, config.Market.Interval, startUtc, endUtc);

        IReadOnlyList<Candle> candles = await marketData.GetCandlesAsync(
            config.Market.Symbol, interval, startUtc, endUtc, ct).ConfigureAwait(false);

        logger.LogInformation("Ingested {Count} candles", candles.Count);
        if (candles.Count > 0)
        {
            logger.LogInformation("Range on disk: {First} .. {Last}",
                candles[0].OpenTimeUtc, candles[^1].OpenTimeUtc);
        }
        return 0;
    }
}
