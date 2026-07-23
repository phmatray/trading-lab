namespace TradyStrat.Domain.Settings.Polymarket;

public sealed record PolymarketSettings(
    SearchQueries SearchQueries,
    MaxMarkets MaxMarkets,
    MinVolumeUsd MinVolumeUsd,
    MaxHorizonDays MaxHorizonDays);
