using TradyStrat.Domain.Suggestions;
using TradyStrat.Application.PredictionMarkets;

namespace TradyStrat.Application.AiSuggestion.Snapshot.Sections;

public sealed partial class MarketsSection(
    IPredictionMarketProvider markets,
    ILogger<MarketsSection> log) : ISnapshotSectionProvider
{
    public int Order => 50;

    public async Task ContributeAsync(SnapshotBuilder builder, int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        try
        {
            builder.Markets = await markets.GetMarketsAsync(ct);
        }
        catch (PolymarketUnavailableException ex)
        {
            LogPolymarketUnavailable(log, ex);
            builder.Markets = [];
        }
        if (builder.Markets.Count == 0)
            LogPolymarketEmpty(log);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Polymarket unavailable, snapshot will omit markets")]
    private static partial void LogPolymarketUnavailable(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Polymarket filter returned 0 markets — adjust Tags / MinVolumeUsd / MaxHorizonDays")]
    private static partial void LogPolymarketEmpty(ILogger logger);
}
