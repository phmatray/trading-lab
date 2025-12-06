using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Strategies;

public abstract class BaseStrategy : IStrategy
{
    protected readonly IIndicatorCalculator _indicatorCalculator;
    protected IReadOnlyList<HistoricalPrice> HistoricalData { get; private set; } = null!;
    protected decimal[] ClosePrices { get; private set; } = null!;

    public abstract string Name { get; }
    public abstract string Description { get; }

    protected BaseStrategy(IIndicatorCalculator indicatorCalculator)
    {
        _indicatorCalculator = indicatorCalculator;
    }

    public virtual void Initialize(IReadOnlyList<HistoricalPrice> historicalData)
    {
        HistoricalData = historicalData;
        ClosePrices = historicalData.Select(h => h.Close ?? 0).ToArray();
    }

    public abstract TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition);

    public abstract Dictionary<string, object> GetParameters();

    // Delegate indicator calculations to IIndicatorCalculator
    protected decimal[] CalculateSMA(int period)
        => _indicatorCalculator.CalculateSMA(ClosePrices, period);

    protected decimal[] CalculateEMA(int period)
        => _indicatorCalculator.CalculateEMA(ClosePrices, period);

    protected decimal[] CalculateRSI(int period)
        => _indicatorCalculator.CalculateRSI(ClosePrices, period);

    protected (decimal[] macd, decimal[] signal, decimal[] histogram) CalculateMACD(
        int fastPeriod = 12,
        int slowPeriod = 26,
        int signalPeriod = 9)
        => _indicatorCalculator.CalculateMACD(ClosePrices, fastPeriod, slowPeriod, signalPeriod);

    protected int CalculateQuantity(decimal cash, decimal price, int currentPosition)
    {
        if (currentPosition > 0)
            return 0;

        return (int)(cash / price);
    }
}
