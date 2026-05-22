using TradyStrat.Application.AiSuggestion.Backfill;
using TradyStrat.Application.AiSuggestion.CallDiff;
using TradyStrat.Application.Indicators;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;

namespace TradyStrat.Application.Dashboard;

public sealed record DashboardViewModel(
    DateOnly Today,
    int EntryNumber,
    PortfolioSnapshot Portfolio,
    GoalConfig Goal,
    SuggestionState? FocusCallState,
    IReadOnlyList<TickerView> Tickers,
    IReadOnlyList<PositionRow> Positions,
    string FocusTicker,
    IReadOnlyList<GrowthPoint> Growth,
    DateOnly? LatestPriceDate,
    GoalPaceVm GoalPace,
    CallDiff CallDiff,
    BackfillStatus BackfillStatus,
    string PriceAsOfRelative,
    string CallAsOfRelative,
    string FxAsOfRelative,
    IReadOnlyDictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries> IndicatorHistories,
    IReadOnlyList<CapitalEvent> CapitalEvents,
    // new — time-travel
    bool IsHistorical,
    DateOnly EarliestTradingDay,
    DateOnly LatestTradingDay,
    DateOnly? PrevTradingDay,
    DateOnly? NextTradingDay)
{
    public MarketSnapshot MarketSnapshot { get; init; } = MarketSnapshot.Empty;
}
