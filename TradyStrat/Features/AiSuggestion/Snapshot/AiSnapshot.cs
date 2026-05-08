using System.Text.Json.Serialization;
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
    // [JsonIgnore]: keeps the user-message JSON byte-identical to Phase 1 for
    // the focus ticker. The AI never sees this field; SuggestionService reads
    // it via property access to set Suggestion.InstrumentId on the persisted
    // entity row. PromptHash already excludes it (see AiSnapshotService.HashPrompt).
    [property: JsonIgnore] int InstrumentId,         // NEW (Phase 2)
    GoalConfig Goal,
    PortfolioSnapshot Portfolio,
    IReadOnlyList<TickerContext> Tickers,
    IReadOnlyList<TradeRecent> RecentTrades,
    decimal? UsdPerEur,
    IReadOnlyList<PredictionMarket> Markets,
    string PromptHash);
