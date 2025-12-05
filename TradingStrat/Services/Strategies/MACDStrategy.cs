namespace TradingStrat.Services.Strategies;

public class MACDStrategy : BaseStrategy
{
    private readonly int _fastPeriod;
    private readonly int _slowPeriod;
    private readonly int _signalPeriod;
    private decimal[] _macd = null!;
    private decimal[] _signal = null!;
    private decimal[] _histogram = null!;

    public override string Name => $"MACD ({_fastPeriod}/{_slowPeriod}/{_signalPeriod})";

    public override string Description =>
        $"Buy when MACD line crosses above signal line. " +
        $"Sell when MACD line crosses below signal line. " +
        $"Parameters: Fast={_fastPeriod}, Slow={_slowPeriod}, Signal={_signalPeriod}";

    public MACDStrategy(int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
    {
        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
        _signalPeriod = signalPeriod;
    }

    public override void Initialize(IReadOnlyList<Models.HistoricalPrice> historicalData)
    {
        base.Initialize(historicalData);
        (_macd, _signal, _histogram) = CalculateMACD(_fastPeriod, _slowPeriod, _signalPeriod);
    }

    public override TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition)
    {
        var requiredBars = _slowPeriod + _signalPeriod;
        if (currentIndex < requiredBars || _macd[currentIndex] == 0 || _signal[currentIndex] == 0)
        {
            return new TradeSignal(SignalType.Hold, 0, 0, "Insufficient data for MACD");
        }

        var currentPrice = ClosePrices[currentIndex];
        var macdCurrent = _macd[currentIndex];
        var signalCurrent = _signal[currentIndex];
        var macdPrevious = _macd[currentIndex - 1];
        var signalPrevious = _signal[currentIndex - 1];

        if (macdPrevious <= signalPrevious && macdCurrent > signalCurrent && currentPosition == 0)
        {
            var quantity = CalculateQuantity(currentCash, currentPrice, currentPosition);
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
