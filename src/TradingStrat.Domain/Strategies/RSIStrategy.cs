using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Strategies;

public class RSIStrategy : BaseStrategy
{
    private readonly int _period;
    private readonly decimal _oversoldThreshold;
    private readonly decimal _overboughtThreshold;
    private decimal[] _rsi = null!;

    public override string Name => $"RSI ({_period}, {_oversoldThreshold}/{_overboughtThreshold})";

    public override string Description =>
        $"Buy when RSI({_period}) crosses above {_oversoldThreshold} (oversold). " +
        $"Sell when RSI({_period}) crosses below {_overboughtThreshold} (overbought).";

    public RSIStrategy(
        IIndicatorCalculator indicatorCalculator,
        int period = 14,
        decimal oversoldThreshold = 30,
        decimal overboughtThreshold = 70)
        : base(indicatorCalculator)
    {
        if (oversoldThreshold >= overboughtThreshold)
            throw new ArgumentException("Oversold threshold must be less than overbought threshold");

        _period = period;
        _oversoldThreshold = oversoldThreshold;
        _overboughtThreshold = overboughtThreshold;
    }

    public override void Initialize(IReadOnlyList<HistoricalPrice> historicalData)
    {
        base.Initialize(historicalData);
        _rsi = CalculateRSI(_period);
    }

    public override TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition)
    {
        if (currentIndex < _period + 1)
        {
            return new TradeSignal(SignalType.Hold, 0, 0, "Insufficient data for RSI");
        }

        var currentPrice = ClosePrices[currentIndex];
        var rsiCurrent = _rsi[currentIndex];
        var rsiPrevious = _rsi[currentIndex - 1];

        if (rsiPrevious <= _oversoldThreshold && rsiCurrent > _oversoldThreshold && currentPosition == 0)
        {
            var quantity = CalculateQuantity(currentCash, currentPrice, currentPosition);
            if (quantity > 0)
            {
                return new TradeSignal(
                    SignalType.Buy,
                    currentPrice,
                    quantity,
                    $"RSI ({rsiCurrent:F2}) crossed above oversold threshold ({_oversoldThreshold})"
                );
            }
        }

        if (rsiPrevious >= _overboughtThreshold && rsiCurrent < _overboughtThreshold && currentPosition > 0)
        {
            return new TradeSignal(
                SignalType.Sell,
                currentPrice,
                currentPosition,
                $"RSI ({rsiCurrent:F2}) crossed below overbought threshold ({_overboughtThreshold})"
            );
        }

        return new TradeSignal(SignalType.Hold, 0, 0, $"RSI at {rsiCurrent:F2}, no signal");
    }

    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            { "Period", _period },
            { "OversoldThreshold", _oversoldThreshold },
            { "OverboughtThreshold", _overboughtThreshold }
        };
    }
}
