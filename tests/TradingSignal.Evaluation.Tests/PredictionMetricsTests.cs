using Shouldly;
using TradingSignal.Core;
using TradingSignal.Evaluation.Metrics;

namespace TradingSignal.Evaluation.Tests;

public sealed class PredictionMetricsTests
{
    private static (Prediction, Outcome) Make(TradeAction action, double confidence, bool correct)
    {
        var id = Guid.NewGuid();
        var asOf = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var features = new FeatureSet(asOf, "X", 0m, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        var p = new Prediction(id, asOf, "X", features, new RawSignal(action, confidence, "r"), 0);
        var o = new Outcome(id, 0m, 0m, 0d, correct);
        return (p, o);
    }

    [Fact]
    public void Brier_Score_On_Hand_Computed_Two_Item_Set()
    {
        // Hand math:
        //  prediction 1: conf 0.8, correct → (0.8 - 1)^2 = 0.04
        //  prediction 2: conf 0.3, wrong   → (0.3 - 0)^2 = 0.09
        //  Brier = (0.04 + 0.09) / 2 = 0.065
        var records = new[]
        {
            Make(TradeAction.Buy, 0.8, correct: true),
            Make(TradeAction.Sell, 0.3, correct: false),
        };

        var scores = PredictionMetrics.Compute(records);

        scores.BrierScore.ShouldBe(0.065, 1e-9);
    }

    [Fact]
    public void Accuracy_Excludes_Hold_Predictions()
    {
        // 1 BUY correct, 1 SELL wrong, 1 HOLD anything → 1/2 = 0.5
        var records = new[]
        {
            Make(TradeAction.Buy, 0.6, correct: true),
            Make(TradeAction.Sell, 0.6, correct: false),
            Make(TradeAction.Hold, 0.4, correct: true),
        };

        var scores = PredictionMetrics.Compute(records);

        scores.Total.ShouldBe(3);
        scores.NonHold.ShouldBe(2);
        scores.CorrectNonHold.ShouldBe(1);
        scores.Accuracy.ShouldBe(0.5);
    }

    [Fact]
    public void Reliability_Bins_Group_Confidence_Into_Deciles()
    {
        var records = new[]
        {
            Make(TradeAction.Buy, 0.05, correct: false), // bin 0
            Make(TradeAction.Buy, 0.55, correct: true),  // bin 5
            Make(TradeAction.Buy, 0.58, correct: true),  // bin 5
            Make(TradeAction.Buy, 0.92, correct: false), // bin 9
            Make(TradeAction.Buy, 0.99, correct: true),  // bin 9
        };

        var scores = PredictionMetrics.Compute(records);

        scores.ReliabilityBins.Count.ShouldBe(10);
        scores.ReliabilityBins[0].Count.ShouldBe(1);
        scores.ReliabilityBins[5].Count.ShouldBe(2);
        scores.ReliabilityBins[5].EmpiricalAccuracy.ShouldBe(1.0);
        scores.ReliabilityBins[9].Count.ShouldBe(2);
        scores.ReliabilityBins[9].EmpiricalAccuracy.ShouldBe(0.5);
    }

    [Fact]
    public void Empty_Input_Returns_Zero_Metrics()
    {
        var scores = PredictionMetrics.Compute(Array.Empty<(Prediction, Outcome)>());

        scores.Total.ShouldBe(0);
        scores.Accuracy.ShouldBe(0d);
        scores.BrierScore.ShouldBe(0d);
        scores.ReliabilityBins.Count.ShouldBe(10);
    }
}
