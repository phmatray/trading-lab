using TradyStrat.Common.Domain;
using TradyStrat.Features.PredictionMarkets;

namespace TradyStrat.Features.AiSuggestion.Snapshot;

public sealed record TickerContext(
    string Ticker, string Currency,
    decimal PriceNative, decimal? PriceEur,
    Zone Zone, IReadOnlyList<string> Reasons);

public sealed record TradeRecent(
    DateOnly ExecutedOn, TradeSide Side,
    decimal Quantity, decimal PricePerShare);

public sealed record AiSnapshot(
    DateOnly Today,
    GoalConfig Goal,
    PortfolioSnapshot Portfolio,
    IReadOnlyList<TickerContext> Tickers,
    IReadOnlyList<TradeRecent> RecentTrades,
    decimal? UsdPerEur,
    IReadOnlyList<PredictionMarket> Markets,
    string PromptHash);
