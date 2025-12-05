namespace TradingStrat.Services.Strategies;

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

    public MovingAverageCrossoverStrategy(int fastPeriod = 20, int slowPeriod = 50)
    {
        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
    }

    public override void Initialize(IReadOnlyList<Models.HistoricalPrice> historicalData)
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

        var currentPrice = ClosePrices[currentIndex];
        var fastCurrent = _fastMA[currentIndex];
        var slowCurrent = _slowMA[currentIndex];
        var fastPrevious = _fastMA[currentIndex - 1];
        var slowPrevious = _slowMA[currentIndex - 1];

        if (fastPrevious <= slowPrevious && fastCurrent > slowCurrent && currentPosition == 0)
        {
            var quantity = CalculateQuantity(currentCash, currentPrice, currentPosition);
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

        if (fastPrevious >= slowPrevious && fastCurrent < slowCurrent && currentPosition > 0)
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
