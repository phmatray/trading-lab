using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Strategies;

public abstract class BaseStrategy(IIndicatorCalculator indicatorCalculator) : IStrategy
{
    protected IReadOnlyList<HistoricalPrice> HistoricalData { get; private set; } = null!;
    protected decimal[] ClosePrices { get; private set; } = null!;

    /// <summary>
    /// The timeframe of the historical data used by this strategy instance.
    /// Populated during Initialize() if all data bars have consistent timeframe.
    /// Null if data contains mixed timeframes (should not happen in normal use).
    /// </summary>
    protected TimeFrame? DataTimeFrame { get; private set; }

    public abstract string Name { get; }
    public abstract string Description { get; }

    public virtual void Initialize(IReadOnlyList<HistoricalPrice> historicalData)
    {
        HistoricalData = historicalData;
        ClosePrices = historicalData.Select(h => h.Close ?? 0).ToArray();

        // Extract timeframe if data is consistent
        if (historicalData.Count > 0)
        {
            TimeFrameUnit firstTimeFrame = historicalData[0].TimeFrame;
            bool allSameTimeFrame = historicalData.All(h => h.TimeFrame == firstTimeFrame);

            if (allSameTimeFrame)
            {
                DataTimeFrame = new TimeFrame { Unit = firstTimeFrame };
            }
        }
    }

    public abstract TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition);

    public abstract Dictionary<string, object> GetParameters();

    // Delegate indicator calculations to IIndicatorCalculator
    protected decimal[] CalculateSMA(int period)
        => indicatorCalculator.CalculateSMA(ClosePrices, period);

    protected decimal[] CalculateEMA(int period)
        => indicatorCalculator.CalculateEMA(ClosePrices, period);

    protected decimal[] CalculateRSI(int period)
        => indicatorCalculator.CalculateRSI(ClosePrices, period);

    protected (decimal[] macd, decimal[] signal, decimal[] histogram) CalculateMACD(
        int fastPeriod = 12,
        int slowPeriod = 26,
        int signalPeriod = 9)
        => indicatorCalculator.CalculateMACD(ClosePrices, fastPeriod, slowPeriod, signalPeriod);

    protected int CalculateQuantity(decimal cash, decimal price, int currentPosition)
    {
        if (currentPosition > 0)
        {
            return 0;
        }

        return (int)(cash / price);
    }
}
