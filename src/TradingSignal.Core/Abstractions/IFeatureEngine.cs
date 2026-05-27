namespace TradingSignal.Core.Abstractions;

public interface IFeatureEngine
{
    // MUST compute using only candles[0..upToIndex]. Any access to candles past
    // upToIndex is a look-ahead leak and invalidates the entire backtest.
    FeatureSet Compute(IReadOnlyList<Candle> candles, int upToIndex);

    int WarmupPeriods { get; }
}
