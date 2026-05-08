using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Dashboard;

public sealed record TickerView(
    string Ticker,
    string Currency,
    decimal Price,
    decimal? PriceEur,
    decimal? DeltaPct,
    Zone Zone,
    IReadOnlyList<decimal> Spark,
    Suggestion? TodaysCall);    // NEW (Phase 2) — non-null for Held with successful call
