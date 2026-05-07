using TradyStrat.Features.AiSuggestion.Backfill;
using TradyStrat.Features.AiSuggestion.CallDiff;
using TradyStrat.Features.Indicators;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Dashboard;

public sealed record DashboardViewModel(
    DateOnly Today,
    int EntryNumber,
    PortfolioSnapshot Portfolio,
    GoalConfig Goal,
    Suggestion? TodaysCall,
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
    DateOnly? NextTradingDay);
