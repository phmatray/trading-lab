namespace TradyStrat.Cli.Mcp.Dto;

public sealed record DashboardSnapshot(
    string Ticker,
    DateOnly AsOfDate,
    MoneyDualCurrency LastClose,
    ZoneBlock Zone,
    IndicatorsBlock Indicators,
    SuggestionBrief? Suggestion,
    PositionBrief Position);

public sealed record MoneyDualCurrency(decimal Usd, decimal Eur, decimal FxRate);

public sealed record ZoneBlock(string Overall, ZoneByIndicator ByIndicator);
public sealed record ZoneByIndicator(string Bollinger, string Rsi, string Sma, string Ichimoku);

public sealed record IndicatorsBlock(
    BollingerBlock Bollinger,
    RsiBlock Rsi,
    SmaBlock Sma,
    IchimokuBlock Ichimoku);

public sealed record BollingerBlock(decimal? Upper, decimal? Mid, decimal? Lower);
public sealed record RsiBlock(decimal? Value);
public sealed record SmaBlock(decimal? Sma50, decimal? Sma200);
public sealed record IchimokuBlock(decimal? Tenkan, decimal? Kijun, decimal? SenkouA, decimal? SenkouB, decimal? Chikou);

public sealed record SuggestionBrief(
    DateOnly Date, string Action, int Conviction,
    string Reasoning, string? EnvelopeHash, string? PromptVersionHash);

public sealed record PositionBrief(
    int Qty, decimal AvgCostEur,
    decimal MarketValueEur, decimal MarketValueUsd,
    decimal UnrealizedPnlEur, decimal UnrealizedPnlUsd);
