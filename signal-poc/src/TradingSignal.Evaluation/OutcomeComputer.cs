using TradingSignal.Core;

namespace TradingSignal.Evaluation;

// Computes the realized Outcome of a prediction given the candle stream and a
// horizon. PURELY for prediction-quality scoring — does not know about portfolio
// state. The portfolio simulator in TradingSignal.Backtest separately decides
// whether a trade actually executed.
public static class OutcomeComputer
{
    public static Outcome Compute(
        Prediction prediction,
        IReadOnlyList<Candle> candles,
        int decisionIndex,
        int horizonCandles,
        double feeBps)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(horizonCandles, 1);
        var entryIndex = decisionIndex + 1;
        var exitIndex = decisionIndex + horizonCandles;
        if (entryIndex >= candles.Count || exitIndex >= candles.Count)
            throw new ArgumentOutOfRangeException(nameof(decisionIndex),
                $"Need candles up to index {exitIndex}, only have {candles.Count}");

        var entry = candles[entryIndex].Open;
        var exit = candles[exitIndex].Close;
        var grossReturn = entry == 0m ? 0d : (double)((exit - entry) / entry);
        var feeRoundTrip = 2d * feeBps / 10_000d;

        var (realized, directionCorrect) = prediction.Signal.Action switch
        {
            TradeAction.Buy => (grossReturn - feeRoundTrip, exit > entry),
            TradeAction.Sell => (-grossReturn - feeRoundTrip, exit < entry),
            TradeAction.Hold => (0d, Math.Abs(grossReturn) <= feeRoundTrip),
            _ => (0d, false),
        };

        return new Outcome(
            PredictionId: prediction.Id,
            EntryPrice: entry,
            ExitPrice: exit,
            RealizedReturnPct: realized,
            DirectionCorrect: directionCorrect);
    }
}
