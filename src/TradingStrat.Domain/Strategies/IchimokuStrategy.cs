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
    private readonly int _tenkanPeriod;
    private readonly int _kijunPeriod;
    private readonly int _senkouBPeriod;
    private readonly int _displacement;
    private readonly IchimokuExitMode _exitMode;
    private readonly IchimokuEntryMode _entryMode;
    private readonly int _crossLookbackDays;
    private readonly decimal _riskPercentage;
    private readonly TimeframeAggregator _timeframeAggregator;

    // Daily indicators (calculated once in Initialize)
    private decimal[] _dailyTenkan = null!;
    private decimal[] _dailyKijun = null!;
    private decimal[] _dailySenkouA = null!;
    private decimal[] _dailySenkouB = null!;
    private decimal[] _dailyChikou = null!;

    // Weekly indicators
    private HistoricalPrice[] _weeklyPrices = null!;
    private decimal[] _weeklyTenkan = null!;
    private decimal[] _weeklyKijun = null!;
    private decimal[] _weeklySenkouA = null!;
    private decimal[] _weeklySenkouB = null!;

    // Weekly trend mapped to each daily bar
    private TrendState[] _weeklyTrendByDailyBar = null!;

    public override string Name =>
        $"Ichimoku ({_tenkanPeriod}/{_kijunPeriod}/{_senkouBPeriod}) {_exitMode} {_entryMode}";

    public override string Description =>
        $"Ichimoku Cloud strategy with multi-timeframe analysis. " +
        $"Entry: {_entryMode}, Exit: {_exitMode}, Risk: {_riskPercentage:P}. " +
        $"Bullish entry when Daily price > Kumo, Tenkan > Kijun, Chikou clear, and Weekly trend bullish.";

    public IchimokuStrategy(
        IIndicatorCalculator indicatorCalculator,
        TimeframeAggregator timeframeAggregator,
        int tenkanPeriod = 9,
        int kijunPeriod = 26,
        int senkouBPeriod = 52,
        int displacement = 26,
        IchimokuExitMode exitMode = IchimokuExitMode.CloseBelowKijun,
        IchimokuEntryMode entryMode = IchimokuEntryMode.AllConditionsOnly,
        int crossLookbackDays = 5,
        decimal riskPercentage = 0.02m)
        : base(indicatorCalculator)
    {
        if (tenkanPeriod <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tenkanPeriod), "Must be > 0");
        }
        if (kijunPeriod <= tenkanPeriod)
        {
            throw new ArgumentException("Kijun period must be > Tenkan period");
        }
        if (senkouBPeriod <= kijunPeriod)
        {
            throw new ArgumentException("Senkou B period must be > Kijun period");
        }
        if (displacement <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(displacement), "Must be > 0");
        }
        if (crossLookbackDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(crossLookbackDays), "Must be > 0");
        }
        if (riskPercentage <= 0 || riskPercentage > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(riskPercentage), "Must be between 0 and 1");
        }

        _indicatorCalculator = indicatorCalculator;
        _tenkanPeriod = tenkanPeriod;
        _kijunPeriod = kijunPeriod;
        _senkouBPeriod = senkouBPeriod;
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
        _dailyTenkan = _indicatorCalculator.CalculateTenkan(dailyPricesArray, _tenkanPeriod);
        _dailyKijun = _indicatorCalculator.CalculateKijun(dailyPricesArray, _kijunPeriod);
        _dailySenkouA = _indicatorCalculator.CalculateSenkouSpanA(_dailyTenkan, _dailyKijun);
        _dailySenkouB = _indicatorCalculator.CalculateSenkouSpanB(dailyPricesArray, _senkouBPeriod);
        _dailyChikou = _indicatorCalculator.CalculateChikouSpan(dailyPricesArray);

        // Aggregate to Weekly timeframe
        _weeklyPrices = _timeframeAggregator.AggregateToWeekly(historicalData);
        _weeklyTenkan = _indicatorCalculator.CalculateTenkan(_weeklyPrices, _tenkanPeriod);
        _weeklyKijun = _indicatorCalculator.CalculateKijun(_weeklyPrices, _kijunPeriod);
        _weeklySenkouA = _indicatorCalculator.CalculateSenkouSpanA(_weeklyTenkan, _weeklyKijun);
        _weeklySenkouB = _indicatorCalculator.CalculateSenkouSpanB(_weeklyPrices, _senkouBPeriod);

        // Map weekly trend state to each daily bar
        _weeklyTrendByDailyBar = MapWeeklyTrendToDaily(historicalData, _weeklyPrices);
    }

    public override TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition)
    {
        // Minimum data requirement: need displacement bars ahead for Senkou Spans
        // and displacement bars behind for Chikou
        int minBars = Math.Max(_senkouBPeriod, _displacement) + _displacement;
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
                    signals.Kijun);

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

        // Get shifted Senkou Span values (look back displacement periods)
        int senkouIndex = currentIndex - _displacement;
        decimal senkouA = senkouIndex >= 0 ? _dailySenkouA[senkouIndex] : 0;
        decimal senkouB = senkouIndex >= 0 ? _dailySenkouB[senkouIndex] : 0;

        // Kumo boundaries
        decimal kumoTop = Math.Max(senkouA, senkouB);
        decimal kumoBottom = Math.Min(senkouA, senkouB);

        // Chikou comparison (price 26 bars ago)
        int chikouCompareIndex = currentIndex - _displacement;
        decimal priceAtChikouPosition = chikouCompareIndex >= 0 ? ClosePrices[chikouCompareIndex] : 0;
        decimal chikou = _dailyChikou[currentIndex];

        // Current Tenkan/Kijun
        decimal tenkan = _dailyTenkan[currentIndex];
        decimal kijun = _dailyKijun[currentIndex];

        // Weekly trend
        TrendState weeklyTrend = _weeklyTrendByDailyBar[currentIndex];

        return new IchimokuSignals(
            Price: price,
            Tenkan: tenkan,
            Kijun: kijun,
            SenkouA: senkouA,
            SenkouB: senkouB,
            Chikou: chikou,
            PriceAtChikouPosition: priceAtChikouPosition,
            KumoTop: kumoTop,
            KumoBottom: kumoBottom,
            PriceAboveKumo: price > kumoTop,
            PriceBelowKumo: price < kumoBottom,
            PriceInKumo: price >= kumoBottom && price <= kumoTop,
            TenkanAboveKijun: tenkan > kijun,
            ChikouAbovePriceHistory: chikou > priceAtChikouPosition,
            WeeklyTrend: weeklyTrend
        );
    }

    private bool ShouldEnter(IchimokuSignals signals, int currentIndex)
    {
        // Entry conditions (all must be true):
        // 1. Price above Kumo
        // 2. Tenkan > Kijun (bullish momentum)
        // 3. Chikou > price history (no resistance)
        // 4. Weekly trend is bullish
        // 5. If RequireRecentCross mode: Tenkan crossed above Kijun in last N days

        if (!signals.PriceAboveKumo)
        {
            return false;
        }
        if (!signals.TenkanAboveKijun)
        {
            return false;
        }
        if (!signals.ChikouAbovePriceHistory)
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
            IchimokuExitMode.CloseBelowKijun => signals.Price < signals.Kijun,
            IchimokuExitMode.PriceIntoKumo => signals.PriceInKumo || signals.PriceBelowKumo,
            IchimokuExitMode.TenkanKijunBearishCross => DetectBearishCross(currentIndex),
            _ => false
        };
    }

    private bool DetectBullishCrossInLast(int currentIndex, int lookbackDays)
    {
        // Look back up to N days for Tenkan crossing above Kijun
        for (int i = 1; i <= lookbackDays && currentIndex - i >= 0; i++)
        {
            int prevIndex = currentIndex - i;
            if (prevIndex < 1)
            {
                break;
            }

            decimal tenkanPrev = _dailyTenkan[prevIndex - 1];
            decimal kijunPrev = _dailyKijun[prevIndex - 1];
            decimal tenkanCurr = _dailyTenkan[prevIndex];
            decimal kijunCurr = _dailyKijun[prevIndex];

            // Bullish cross: Tenkan was <= Kijun, then crosses above
            if (tenkanPrev <= kijunPrev && tenkanCurr > kijunCurr)
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

        decimal tenkanPrev = _dailyTenkan[currentIndex - 1];
        decimal kijunPrev = _dailyKijun[currentIndex - 1];
        decimal tenkanCurr = _dailyTenkan[currentIndex];
        decimal kijunCurr = _dailyKijun[currentIndex];

        // Bearish cross: Tenkan was >= Kijun, now crosses below
        return tenkanPrev >= kijunPrev && tenkanCurr < kijunCurr;
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

            if (weeklyIndex >= 0 && weeklyIndex < _weeklyTenkan.Length)
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

        // Get shifted Senkou values
        int senkouIndex = weeklyIndex - _displacement;
        if (senkouIndex < 0)
        {
            return TrendState.Neutral;
        }

        decimal senkouA = _weeklySenkouA[senkouIndex];
        decimal senkouB = _weeklySenkouB[senkouIndex];
        decimal kumoTop = Math.Max(senkouA, senkouB);
        decimal kumoBottom = Math.Min(senkouA, senkouB);

        decimal tenkan = _weeklyTenkan[weeklyIndex];
        decimal kijun = _weeklyKijun[weeklyIndex];

        // Bullish: Price > Kumo AND Tenkan > Kijun
        if (weeklyPrice > kumoTop && tenkan > kijun)
        {
            return TrendState.Bullish;
        }

        // Bearish: Price < Kumo OR Tenkan < Kijun
        if (weeklyPrice < kumoBottom || tenkan < kijun)
        {
            return TrendState.Bearish;
        }

        return TrendState.Neutral;
    }

    private string GetEntryReason(IchimokuSignals signals)
    {
        return $"Ichimoku bullish entry - " +
               $"Price {signals.Price:F2} > Kumo ({signals.KumoBottom:F2}-{signals.KumoTop:F2}), " +
               $"Tenkan {signals.Tenkan:F2} > Kijun {signals.Kijun:F2}, " +
               $"Weekly trend: {signals.WeeklyTrend}";
    }

    private static string GetExitReason(IchimokuSignals signals, IchimokuExitMode mode)
    {
        return mode switch
        {
            IchimokuExitMode.CloseBelowKijun =>
                $"Price {signals.Price:F2} closed below Kijun {signals.Kijun:F2}",
            IchimokuExitMode.PriceIntoKumo =>
                $"Price {signals.Price:F2} entered Kumo ({signals.KumoBottom:F2}-{signals.KumoTop:F2})",
            IchimokuExitMode.TenkanKijunBearishCross =>
                $"Tenkan {signals.Tenkan:F2} crossed below Kijun {signals.Kijun:F2}",
            _ => "Exit condition met"
        };
    }

    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            { "TenkanPeriod", _tenkanPeriod },
            { "KijunPeriod", _kijunPeriod },
            { "SenkouBPeriod", _senkouBPeriod },
            { "Displacement", _displacement },
            { "ExitMode", _exitMode.ToString() },
            { "EntryMode", _entryMode.ToString() },
            { "CrossLookbackDays", _crossLookbackDays },
            { "RiskPercentage", _riskPercentage }
        };
    }
}
