using TradingStrat.Services.Strategies;

namespace TradingStrat.Services.Strategies.MachineLearning;

public class PredictionThresholds
{
    public decimal BuyThreshold { get; init; } = 0.01m;    // +1%
    public decimal SellThreshold { get; init; } = -0.01m;  // -1%

    public PredictionThresholds() { }

    public PredictionThresholds(decimal buyThreshold, decimal sellThreshold)
    {
        BuyThreshold = buyThreshold;
        SellThreshold = sellThreshold;
    }

    public SignalType ConvertPredictionToSignal(float predictedReturn)
    {
        if (predictedReturn >= (float)BuyThreshold)
            return SignalType.Buy;
        else if (predictedReturn <= (float)SellThreshold)
            return SignalType.Sell;
        else
            return SignalType.Hold;
    }
}
