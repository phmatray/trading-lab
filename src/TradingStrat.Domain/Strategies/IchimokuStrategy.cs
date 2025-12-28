using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Strategies;

/// <summary>
/// Ichimoku Cloud trading strategy with multi-timeframe analysis.
/// Combines Daily signals with Weekly trend filter for high-probability trades.
/// Supports configurable exit modes and risk-based position sizing.
/// </summary>
public class IchimokuStrategy : BaseStrategy
{
    private readonly IIndicatorCalculator _indicatorCalculator;
    private readonly int _conversionLinePeriod;
    private readonly int _baseLinePeriod;
    private readonly int _leadingSpanBPeriod;
    private readonly int _displacement;
    private readonly IchimokuExitMode _exitMode;
    private readonly IchimokuEntryMode _entryMode;
    private readonly int _crossLookbackDays;
    private readonly decimal _riskPercentage;
    private readonly TimeFrameAggregator _timeframeAggregator;

    // Daily indicators (calculated once in Initialize)
    private decimal[] _dailyConversionLine = null!;
    private decimal[] _dailyBaseLine = null!;
    private decimal[] _dailyLeadingSpanA = null!;
    private decimal[] _dailyLeadingSpanB = null!;
    private decimal[] _dailyLaggingSpan = null!;

    // Weekly indicators
    private HistoricalPrice[] _weeklyPrices = null!;
    private decimal[] _weeklyConversionLine = null!;
    private decimal[] _weeklyBaseLine = null!;
    private decimal[] _weeklyLeadingSpanA = null!;
    private decimal[] _weeklyLeadingSpanB = null!;

    // Weekly trend mapped to each daily bar
    private TrendState[] _weeklyTrendByDailyBar = null!;

    public override string Name =>
        $"Ichimoku ({_conversionLinePeriod}/{_baseLinePeriod}/{_leadingSpanBPeriod}) {_exitMode} {_entryMode}";

    public override string Description =>
        $"Ichimoku Cloud strategy with multi-timeframe analysis. " +
        $"Entry: {_entryMode}, Exit: {_exitMode}, Risk: {_riskPercentage:P}. " +
        $"Bullish entry when Daily price > Kumo, Tenkan > Kijun, Chikou clear, and Weekly trend bullish.";

    public IchimokuStrategy(
        IIndicatorCalculator indicatorCalculator,
        TimeFrameAggregator timeframeAggregator,
        int conversionLinePeriod = 9,
        int baseLinePeriod = 26,
        int leadingSpanBPeriod = 52,
        int displacement = 26,
        IchimokuExitMode exitMode = IchimokuExitMode.CloseBelowBaseLine,
        IchimokuEntryMode entryMode = IchimokuEntryMode.AllConditionsOnly,
        int crossLookbackDays = 5,
        decimal riskPercentage = 0.02m)
        : base(indicatorCalculator)
    {
        ValidationGuard.Require(conversionLinePeriod).GreaterThan(0, "Conversion Line period must be greater than 0");
        ValidationGuard.Require(baseLinePeriod).GreaterThan(conversionLinePeriod, "Base Line period must be greater than Conversion Line period");
        ValidationGuard.Require(leadingSpanBPeriod).GreaterThan(baseLinePeriod, "Leading Span B period must be greater than Base Line period");
        ValidationGuard.Require(displacement).GreaterThan(0, "Displacement must be greater than 0");
        ValidationGuard.Require(crossLookbackDays).GreaterThan(0, "Cross lookback days must be greater than 0");
        ValidationGuard.Require(riskPercentage)
            .GreaterThan(0m, "Risk percentage must be greater than 0")
            .LessThanOrEqual(1m, "Risk percentage cannot exceed 1");

        _indicatorCalculator = indicatorCalculator;
        _conversionLinePeriod = conversionLinePeriod;
        _baseLinePeriod = baseLinePeriod;
        _leadingSpanBPeriod = leadingSpanBPeriod;
        _displacement = displacement;
        _exitMode = exitMode;
        _entryMode = entryMode;
        _crossLookbackDays = crossLookbackDays;
        _riskPercentage = riskPercentage;
        _timeframeAggregator = timeframeAggregator;
    }

    public override void Initialize(IReadOnlyList<HistoricalPrice> historicalData)
    {
        base.Initialize(historicalData);

        // Calculate Daily Ichimoku indicators
        HistoricalPrice[] dailyPricesArray = historicalData.ToArray();
        _dailyConversionLine = _indicatorCalculator.CalculateConversionLine(dailyPricesArray, _conversionLinePeriod);
        _dailyBaseLine = _indicatorCalculator.CalculateBaseLine(dailyPricesArray, _baseLinePeriod);
        _dailyLeadingSpanA = _indicatorCalculator.CalculateLeadingSpanA(_dailyConversionLine, _dailyBaseLine);
        _dailyLeadingSpanB = _indicatorCalculator.CalculateLeadingSpanB(dailyPricesArray, _leadingSpanBPeriod);
        _dailyLaggingSpan = _indicatorCalculator.CalculateLaggingSpan(dailyPricesArray);

        // Aggregate to Weekly timeframe
        _weeklyPrices = _timeframeAggregator.AggregateToWeekly(historicalData);
        _weeklyConversionLine = _indicatorCalculator.CalculateConversionLine(_weeklyPrices, _conversionLinePeriod);
        _weeklyBaseLine = _indicatorCalculator.CalculateBaseLine(_weeklyPrices, _baseLinePeriod);
        _weeklyLeadingSpanA = _indicatorCalculator.CalculateLeadingSpanA(_weeklyConversionLine, _weeklyBaseLine);
        _weeklyLeadingSpanB = _indicatorCalculator.CalculateLeadingSpanB(_weeklyPrices, _leadingSpanBPeriod);

        // Map weekly trend state to each daily bar
        _weeklyTrendByDailyBar = MapWeeklyTrendToDaily(historicalData, _weeklyPrices);
    }

    public override TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition)
    {
        // Minimum data requirement: need displacement bars ahead for Leading Spans
        // and displacement bars behind for Lagging Span
        int minBars = Math.Max(_leadingSpanBPeriod, _displacement) + _displacement;
        if (currentIndex < minBars)
        {
            return new TradeSignal(SignalType.Hold, 0, 0, "Insufficient data for Ichimoku");
        }

        decimal currentPrice = ClosePrices[currentIndex];

        // Build Ichimoku signal state
        IchimokuSignals signals = BuildIchimokuSignals(currentIndex);

        // Check for exit conditions first (if we have a position)
        if (currentPosition > 0)
        {
            bool shouldExit = ShouldExit(signals, currentIndex);
            if (shouldExit)
            {
                return new TradeSignal(
                    SignalType.Sell,
                    currentPrice,
                    currentPosition,
                    GetExitReason(signals, _exitMode)
                );
            }
        }

        // Check for entry conditions (if no position)
        if (currentPosition == 0)
        {
            bool shouldEnter = ShouldEnter(signals, currentIndex);
            if (shouldEnter)
            {
                int quantity = CalculateRiskBasedQuantity(
                    currentCash,
                    currentPrice,
                    signals.BaseLine);

                if (quantity > 0)
                {
                    return new TradeSignal(
                        SignalType.Buy,
                        currentPrice,
                        quantity,
                        GetEntryReason(signals)
                    );
                }
            }
        }

        return new TradeSignal(
            SignalType.Hold,
            0,
            0,
            $"Ichimoku neutral - Price: {currentPrice:F2}, Kumo: {signals.KumoBottom:F2}-{signals.KumoTop:F2}"
        );
    }

    private IchimokuSignals BuildIchimokuSignals(int currentIndex)
    {
        decimal price = ClosePrices[currentIndex];

        // Get shifted Leading Span values (look back displacement periods)
        int leadingSpanIndex = currentIndex - _displacement;
        decimal leadingSpanA = leadingSpanIndex >= 0 ? _dailyLeadingSpanA[leadingSpanIndex] : 0;
        decimal leadingSpanB = leadingSpanIndex >= 0 ? _dailyLeadingSpanB[leadingSpanIndex] : 0;

        // Kumo boundaries
        decimal kumoTop = Math.Max(leadingSpanA, leadingSpanB);
        decimal kumoBottom = Math.Min(leadingSpanA, leadingSpanB);

        // Lagging Span comparison (price 26 bars ago)
        int laggingSpanCompareIndex = currentIndex - _displacement;
        decimal priceAtLaggingSpanPosition = laggingSpanCompareIndex >= 0 ? ClosePrices[laggingSpanCompareIndex] : 0;
        decimal laggingSpan = _dailyLaggingSpan[currentIndex];

        // Current Conversion Line/Base Line
        decimal conversionLine = _dailyConversionLine[currentIndex];
        decimal baseLine = _dailyBaseLine[currentIndex];

        // Weekly trend
        TrendState weeklyTrend = _weeklyTrendByDailyBar[currentIndex];

        return new IchimokuSignals(
            price: price,
            conversionLine: conversionLine,
            baseLine: baseLine,
            leadingSpanA: leadingSpanA,
            leadingSpanB: leadingSpanB,
            laggingSpan: laggingSpan,
            priceAtLaggingSpanPosition: priceAtLaggingSpanPosition,
            kumoTop: kumoTop,
            kumoBottom: kumoBottom,
            priceAboveKumo: price > kumoTop,
            priceBelowKumo: price < kumoBottom,
            priceInKumo: price >= kumoBottom && price <= kumoTop,
            conversionLineAboveBaseLine: conversionLine > baseLine,
            laggingSpanAbovePriceHistory: laggingSpan > priceAtLaggingSpanPosition,
            weeklyTrend: weeklyTrend
        );
    }

    private bool ShouldEnter(IchimokuSignals signals, int currentIndex)
    {
        // Entry conditions (all must be true):
        // 1. Price above Kumo
        // 2. Conversion Line > Base Line (bullish momentum)
        // 3. Lagging Span > price history (no resistance)
        // 4. Weekly trend is bullish
        // 5. If RequireRecentCross mode: Conversion Line crossed above Base Line in last N days

        if (!signals.PriceAboveKumo)
        {
            return false;
        }
        if (!signals.ConversionLineAboveBaseLine)
        {
            return false;
        }
        if (!signals.LaggingSpanAbovePriceHistory)
        {
            return false;
        }
        if (signals.WeeklyTrend != TrendState.Bullish)
        {
            return false;
        }

        // Check for recent cross if required
        if (_entryMode == IchimokuEntryMode.RequireRecentCross)
        {
            bool recentCross = DetectBullishCrossInLast(currentIndex, _crossLookbackDays);
            if (!recentCross)
            {
                return false;
            }
        }

        return true;
    }

    private bool ShouldExit(IchimokuSignals signals, int currentIndex)
    {
        return _exitMode switch
        {
            IchimokuExitMode.CloseBelowBaseLine => signals.Price < signals.BaseLine,
            IchimokuExitMode.PriceIntoKumo => signals.PriceInKumo || signals.PriceBelowKumo,
            IchimokuExitMode.ConversionBaseBearishCross => DetectBearishCross(currentIndex),
            _ => false
        };
    }

    private bool DetectBullishCrossInLast(int currentIndex, int lookbackDays)
    {
        // Look back up to N days for Conversion Line crossing above Base Line
        for (int i = 1; i <= lookbackDays && currentIndex - i >= 0; i++)
        {
            int prevIndex = currentIndex - i;
            if (prevIndex < 1)
            {
                break;
            }

            decimal conversionLinePrev = _dailyConversionLine[prevIndex - 1];
            decimal baseLinePrev = _dailyBaseLine[prevIndex - 1];
            decimal conversionLineCurr = _dailyConversionLine[prevIndex];
            decimal baseLineCurr = _dailyBaseLine[prevIndex];

            // Bullish cross: Conversion Line was <= Base Line, then crosses above
            if (conversionLinePrev <= baseLinePrev && conversionLineCurr > baseLineCurr)
            {
                return true;
            }
        }

        return false;
    }

    private bool DetectBearishCross(int currentIndex)
    {
        if (currentIndex < 1)
        {
            return false;
        }

        decimal conversionLinePrev = _dailyConversionLine[currentIndex - 1];
        decimal baseLinePrev = _dailyBaseLine[currentIndex - 1];
        decimal conversionLineCurr = _dailyConversionLine[currentIndex];
        decimal baseLineCurr = _dailyBaseLine[currentIndex];

        // Bearish cross: Conversion Line was >= Base Line, now crosses below
        return conversionLinePrev >= baseLinePrev && conversionLineCurr < baseLineCurr;
    }

    private int CalculateRiskBasedQuantity(decimal cash, decimal entryPrice, decimal kijunStop)
    {
        // Risk-based position sizing:
        // Position size = (Account Risk) / (Entry - Stop Distance)
        // Account Risk = Cash * Risk Percentage
        // Stop Distance = Entry Price - Kijun (stop placed at Kijun)

        decimal stopDistance = Math.Abs(entryPrice - kijunStop);
        if (stopDistance <= 0)
        {
            return 0; // Invalid stop
        }

        decimal accountRisk = cash * _riskPercentage;
        int quantity = (int)(accountRisk / stopDistance);

        // Ensure we don't exceed available cash
        int maxQuantity = (int)(cash / entryPrice);
        return Math.Min(quantity, maxQuantity);
    }

    private TrendState[] MapWeeklyTrendToDaily(
        IReadOnlyList<HistoricalPrice> dailyPrices,
        HistoricalPrice[] weeklyPrices)
    {
        TrendState[] dailyTrends = new TrendState[dailyPrices.Count];

        // For each daily bar, find corresponding weekly bar and determine trend
        for (int i = 0; i < dailyPrices.Count; i++)
        {
            DateTime dailyDate = dailyPrices[i].DateTime;

            // Find weekly bar that contains this daily bar
            int weeklyIndex = FindWeeklyBarIndex(dailyDate, weeklyPrices);

            if (weeklyIndex >= 0 && weeklyIndex < _weeklyConversionLine.Length)
            {
                dailyTrends[i] = DetermineWeeklyTrend(weeklyIndex);
            }
            else
            {
                dailyTrends[i] = TrendState.Neutral;
            }
        }

        return dailyTrends;
    }

    private static int FindWeeklyBarIndex(DateTime dailyDate, HistoricalPrice[] weeklyPrices)
    {
        // Find the weekly bar that contains this daily date
        // Weekly bar DateTime is the last day of the week
        for (int i = 0; i < weeklyPrices.Length; i++)
        {
            DateTime weekEnd = weeklyPrices[i].DateTime;
            DateTime weekStart = weekEnd.AddDays(-6); // Approximate week start

            if (dailyDate >= weekStart && dailyDate <= weekEnd)
            {
                return i;
            }
        }

        // If not found, use last available weekly bar if daily date is after
        if (weeklyPrices.Length > 0 && dailyDate > weeklyPrices[^1].DateTime)
        {
            return weeklyPrices.Length - 1;
        }

        return -1;
    }

    private TrendState DetermineWeeklyTrend(int weeklyIndex)
    {
        if (weeklyIndex < _displacement)
        {
            return TrendState.Neutral;
        }

        decimal weeklyPrice = _weeklyPrices[weeklyIndex].Close ?? 0;

        // Get shifted Leading Span values
        int leadingSpanIndex = weeklyIndex - _displacement;
        if (leadingSpanIndex < 0)
        {
            return TrendState.Neutral;
        }

        decimal leadingSpanA = _weeklyLeadingSpanA[leadingSpanIndex];
        decimal leadingSpanB = _weeklyLeadingSpanB[leadingSpanIndex];
        decimal kumoTop = Math.Max(leadingSpanA, leadingSpanB);
        decimal kumoBottom = Math.Min(leadingSpanA, leadingSpanB);

        decimal conversionLine = _weeklyConversionLine[weeklyIndex];
        decimal baseLine = _weeklyBaseLine[weeklyIndex];

        // Bullish: Price > Kumo AND Conversion Line > Base Line
        if (weeklyPrice > kumoTop && conversionLine > baseLine)
        {
            return TrendState.Bullish;
        }

        // Bearish: Price < Kumo OR Conversion Line < Base Line
        if (weeklyPrice < kumoBottom || conversionLine < baseLine)
        {
            return TrendState.Bearish;
        }

        return TrendState.Neutral;
    }

    private string GetEntryReason(IchimokuSignals signals)
    {
        return $"Ichimoku bullish entry - " +
               $"Price {signals.Price:F2} > Kumo ({signals.KumoBottom:F2}-{signals.KumoTop:F2}), " +
               $"Conversion Line {signals.ConversionLine:F2} > Base Line {signals.BaseLine:F2}, " +
               $"Weekly trend: {signals.WeeklyTrend}";
    }

    private static string GetExitReason(IchimokuSignals signals, IchimokuExitMode mode)
    {
        return mode switch
        {
            IchimokuExitMode.CloseBelowBaseLine =>
                $"Price {signals.Price:F2} closed below Base Line {signals.BaseLine:F2}",
            IchimokuExitMode.PriceIntoKumo =>
                $"Price {signals.Price:F2} entered Kumo ({signals.KumoBottom:F2}-{signals.KumoTop:F2})",
            IchimokuExitMode.ConversionBaseBearishCross =>
                $"Conversion Line {signals.ConversionLine:F2} crossed below Base Line {signals.BaseLine:F2}",
            _ => "Exit condition met"
        };
    }

    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            { "ConversionLinePeriod", _conversionLinePeriod },
            { "BaseLinePeriod", _baseLinePeriod },
            { "LeadingSpanBPeriod", _leadingSpanBPeriod },
            { "Displacement", _displacement },
            { "ExitMode", _exitMode.ToString() },
            { "EntryMode", _entryMode.ToString() },
            { "CrossLookbackDays", _crossLookbackDays },
            { "RiskPercentage", _riskPercentage }
        };
    }
}
