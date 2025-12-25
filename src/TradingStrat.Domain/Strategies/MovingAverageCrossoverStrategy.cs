using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Strategies;

public class MovingAverageCrossoverStrategy : BaseStrategy
{
    private readonly int _fastPeriod;
    private readonly int _slowPeriod;
    private decimal[] _fastMA = null!;
    private decimal[] _slowMA = null!;

    public override string Name => $"MA Crossover ({_fastPeriod}/{_slowPeriod})";

    public override string Description =>
        $"Buy when {_fastPeriod}-period MA crosses above {_slowPeriod}-period MA. " +
        $"Sell when {_fastPeriod}-period MA crosses below {_slowPeriod}-period MA.";

    public MovingAverageCrossoverStrategy(
        IIndicatorCalculator indicatorCalculator,
        int fastPeriod = 20,
        int slowPeriod = 50)
        : base(indicatorCalculator)
    {
        ValidationGuard.Require(fastPeriod)
            .GreaterThan(0, "Fast period must be greater than 0")
            .LessThan(slowPeriod, "Fast period must be less than slow period");
        ValidationGuard.Require(slowPeriod)
            .GreaterThan(fastPeriod, "Slow period must be greater than fast period");

        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
    }

    public override void Initialize(IReadOnlyList<HistoricalPrice> historicalData)
    {
        base.Initialize(historicalData);
        _fastMA = CalculateSMA(_fastPeriod);
        _slowMA = CalculateSMA(_slowPeriod);
    }

    public override TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition)
    {
        if (currentIndex < _slowPeriod || _fastMA[currentIndex] == 0 || _slowMA[currentIndex] == 0)
        {
            return new TradeSignal(SignalType.Hold, 0, 0, "Insufficient data for indicators");
        }

        decimal currentPrice = ClosePrices[currentIndex];
        decimal fastCurrent = _fastMA[currentIndex];
        decimal slowCurrent = _slowMA[currentIndex];

        // Buy when fast MA crosses above slow MA
        if (CrossoverDetector.DetectCrossBetween(_fastMA, _slowMA, currentIndex, CrossDirection.Above)
            && currentPosition == 0)
        {
            int quantity = CalculateQuantity(currentCash, currentPrice, currentPosition);
            if (quantity > 0)
            {
                return new TradeSignal(
                    SignalType.Buy,
                    currentPrice,
                    quantity,
                    $"Fast MA ({fastCurrent:F2}) crossed above Slow MA ({slowCurrent:F2})"
                );
            }
        }

        // Sell when fast MA crosses below slow MA
        if (CrossoverDetector.DetectCrossBetween(_fastMA, _slowMA, currentIndex, CrossDirection.Below)
            && currentPosition > 0)
        {
            return new TradeSignal(
                SignalType.Sell,
                currentPrice,
                currentPosition,
                $"Fast MA ({fastCurrent:F2}) crossed below Slow MA ({slowCurrent:F2})"
            );
        }

        return new TradeSignal(SignalType.Hold, 0, 0, "No crossover detected");
    }

    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            { "FastPeriod", _fastPeriod },
            { "SlowPeriod", _slowPeriod }
        };
    }
}
