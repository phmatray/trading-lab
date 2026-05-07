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
    Suggestion TodaysCall,
    IReadOnlyList<TickerView> Tickers,
    IReadOnlyList<GrowthPoint> Growth,
    DateOnly? LatestPriceDate,
    // new for spec 1
    GoalPaceVm GoalPace,
    CallDiff CallDiff,
    BackfillStatus BackfillStatus,
    string PriceAsOfRelative,
    string CallAsOfRelative,
    string FxAsOfRelative,
    IReadOnlyDictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries> IndicatorHistories);
