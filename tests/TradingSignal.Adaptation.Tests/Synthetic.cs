using TradingSignal.Adaptation;
using TradingSignal.Core;

namespace TradingSignal.Adaptation.Tests;

internal static class Synthetic
{
    public static FeatureSet Features(double rsi = 50, double conf = 0.5,
        double return5 = 0, double volatility = 1.0)
    {
        DateTime t = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return new FeatureSet(t, "BTCUSDT", 50_000m,
            Rsi14: rsi, MacdLine: 0, MacdSignal: 0, MacdHistogram: 0,
            Ema20: 50_000, Ema50: 49_500, Atr14: 100,
            Return1: 0, Return5: return5, VolatilityPct: volatility);
    }

    public static AdaptationSample Sample(double confidence, TradeAction action, double realizedReturn, double rsi = 50, double return5 = 0)
    {
        FeatureSet f = Features(rsi: rsi, conf: confidence, return5: return5);
        RawSignal sig = new(action, confidence, "syn");
        Outcome o = new(Guid.Empty, 100m, 100m, realizedReturn, realizedReturn > 0);
        return new AdaptationSample(CandleIndex: 0, Features: f, Signal: sig, Outcome: o);
    }
}
