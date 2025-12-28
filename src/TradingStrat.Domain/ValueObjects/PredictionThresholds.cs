using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

public sealed class PredictionThresholds : ValueObject
{
    public decimal BuyThreshold { get; init; } = 0.01m;    // +1%
    public decimal SellThreshold { get; init; } = -0.01m;  // -1%

    public PredictionThresholds() { }

    public PredictionThresholds(decimal BuyThreshold, decimal SellThreshold)
    {
        this.BuyThreshold = BuyThreshold;
        this.SellThreshold = SellThreshold;
    }

    public SignalType ConvertPredictionToSignal(float predictedReturn)
    {
        if (predictedReturn >= (float)BuyThreshold)
        {
            return SignalType.Buy;
        }
        else if (predictedReturn <= (float)SellThreshold)
        {
            return SignalType.Sell;
        }
        else
        {
            return SignalType.Hold;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return BuyThreshold;
        yield return SellThreshold;
    }
}
