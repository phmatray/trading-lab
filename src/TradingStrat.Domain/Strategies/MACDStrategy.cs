using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Strategies;

public class MACDStrategy : BaseStrategy
{
    private readonly int _fastPeriod;
    private readonly int _slowPeriod;
    private readonly int _signalPeriod;
    private decimal[] _macd = null!;
    private decimal[] _signal = null!;

    public override string Name => $"MACD ({_fastPeriod}/{_slowPeriod}/{_signalPeriod})";

    public override string Description =>
        $"Buy when MACD line crosses above signal line. " +
        $"Sell when MACD line crosses below signal line. " +
        $"Parameters: Fast={_fastPeriod}, Slow={_slowPeriod}, Signal={_signalPeriod}";

    public MACDStrategy(
        IIndicatorCalculator indicatorCalculator,
        int fastPeriod = 12,
        int slowPeriod = 26,
        int signalPeriod = 9)
        : base(indicatorCalculator)
    {
        ValidationGuard.Require(fastPeriod)
            .GreaterThan(0, "Fast period must be greater than 0")
            .LessThan(slowPeriod, "Fast period must be less than slow period");
        ValidationGuard.Require(slowPeriod)
            .GreaterThan(fastPeriod, "Slow period must be greater than fast period");
        ValidationGuard.Require(signalPeriod)
            .GreaterThan(0, "Signal period must be greater than 0");

        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
        _signalPeriod = signalPeriod;
    }

    public override void Initialize(IReadOnlyList<HistoricalPrice> historicalData)
    {
        base.Initialize(historicalData);
        (_macd, _signal, _) = CalculateMACD(_fastPeriod, _slowPeriod, _signalPeriod);
    }

    public override TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition)
    {
        int requiredBars = _slowPeriod + _signalPeriod;
        if (currentIndex < requiredBars || _macd[currentIndex] == 0 || _signal[currentIndex] == 0)
        {
            return new TradeSignal(SignalType.Hold, 0, 0, "Insufficient data for MACD");
        }

        decimal currentPrice = ClosePrices[currentIndex];
        decimal macdCurrent = _macd[currentIndex];
        decimal signalCurrent = _signal[currentIndex];
        decimal macdPrevious = _macd[currentIndex - 1];
        decimal signalPrevious = _signal[currentIndex - 1];

        if (macdPrevious <= signalPrevious && macdCurrent > signalCurrent && currentPosition == 0)
        {
            int quantity = CalculateQuantity(currentCash, currentPrice, currentPosition);
            if (quantity > 0)
            {
                return new TradeSignal(
                    SignalType.Buy,
                    currentPrice,
                    quantity,
                    $"MACD ({macdCurrent:F4}) crossed above Signal ({signalCurrent:F4})"
                );
            }
        }

        if (macdPrevious >= signalPrevious && macdCurrent < signalCurrent && currentPosition > 0)
        {
            return new TradeSignal(
                SignalType.Sell,
                currentPrice,
                currentPosition,
                $"MACD ({macdCurrent:F4}) crossed below Signal ({signalCurrent:F4})"
            );
        }

        return new TradeSignal(SignalType.Hold, 0, 0, "No MACD crossover detected");
    }

    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            { "FastPeriod", _fastPeriod },
            { "SlowPeriod", _slowPeriod },
            { "SignalPeriod", _signalPeriod }
        };
    }
}
