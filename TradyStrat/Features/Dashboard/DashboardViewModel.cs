using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard;

public sealed record DashboardViewModel(
    DateOnly Today,
    int EntryNumber,
    PortfolioSnapshot Portfolio,
    GoalConfig Goal,
    Suggestion TodaysCall,
    IReadOnlyList<TickerView> Tickers,
    IReadOnlyList<GrowthPoint> Growth,
    DateOnly? LatestPriceDate);
