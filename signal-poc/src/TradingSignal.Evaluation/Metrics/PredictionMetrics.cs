using TradingSignal.Core;

namespace TradingSignal.Evaluation.Metrics;

public sealed record PredictionScores(
    int Total,
    int NonHold,
    int CorrectNonHold,
    double Accuracy,
    double BrierScore,
    IReadOnlyList<ReliabilityBin> ReliabilityBins);

public static class PredictionMetrics
{
    private const int BinCount = 10;

    public static PredictionScores Compute(IReadOnlyList<(Prediction Prediction, Outcome Outcome)> records)
    {
        var total = records.Count;
        var nonHold = 0;
        var correctNonHold = 0;
        var brierSum = 0d;

        var binCounts = new int[BinCount];
        var binCorrect = new int[BinCount];
        var binConfSum = new double[BinCount];

        foreach (var (p, o) in records)
        {
            var target = o.DirectionCorrect ? 1d : 0d;
            var err = p.Signal.Confidence - target;
            brierSum += err * err;

            if (p.Signal.Action != TradeAction.Hold)
            {
                nonHold++;
                if (o.DirectionCorrect) correctNonHold++;
            }

            var idx = BinIndex(p.Signal.Confidence);
            binCounts[idx]++;
            binConfSum[idx] += p.Signal.Confidence;
            if (o.DirectionCorrect) binCorrect[idx]++;
        }

        var accuracy = nonHold == 0 ? 0d : (double)correctNonHold / nonHold;
        var brier = total == 0 ? 0d : brierSum / total;

        var bins = new List<ReliabilityBin>(BinCount);
        for (var i = 0; i < BinCount; i++)
        {
            var count = binCounts[i];
            bins.Add(new ReliabilityBin(
                Index: i,
                Lower: i / (double)BinCount,
                Upper: (i + 1) / (double)BinCount,
                Count: count,
                MeanConfidence: count == 0 ? 0d : binConfSum[i] / count,
                EmpiricalAccuracy: count == 0 ? 0d : (double)binCorrect[i] / count));
        }

        return new PredictionScores(total, nonHold, correctNonHold, accuracy, brier, bins);
    }

    private static int BinIndex(double confidence)
    {
        if (double.IsNaN(confidence) || confidence < 0d) return 0;
        if (confidence >= 1d) return BinCount - 1;
        return (int)(confidence * BinCount);
    }
}
