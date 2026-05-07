using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Dashboard;

public sealed record TickerView(
    string Ticker,
    string Currency,
    decimal Price,
    decimal? PriceEur,
    decimal? DeltaPct,
    Zone Zone,
    IReadOnlyList<decimal> Spark);
