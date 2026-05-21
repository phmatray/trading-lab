using TradyStrat.Domain;

namespace TradyStrat.Application.Dashboard;

public sealed record TickerView(
    int InstrumentId,
    string Ticker,
    string Currency,
    decimal Price,
    decimal? PriceEur,
    decimal? DeltaPct,
    Zone Zone,
    IReadOnlyList<decimal> Spark,
    SuggestionState? CallState);   // null = no AI expected (watchlist or historical-missing)
