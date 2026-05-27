using Shouldly;
using TradingSignal.Core;

namespace TradingSignal.Adaptation.Tests;

public sealed class ThresholdOptimizerTests
{
    [Fact]
    public void PickBestThreshold_Prefers_Higher_Threshold_When_Only_High_Confidence_Signals_Are_Profitable()
    {
        // Construct samples where only confidence >= 0.8 has positive returns;
        // anything below 0.8 has noisy near-zero returns.
        List<AdaptationSample> samples = new();
        for (int i = 0; i < 200; i++)
        {
            double conf = (i % 10) / 10d + 0.05;  // 0.05, 0.15, ..., 0.95
            double pnl = conf >= 0.8 ? 0.01 : -0.002;
            samples.Add(Synthetic.Sample(conf, TradeAction.Buy, pnl));
        }

        double tau = ThresholdOptimizer.PickBestThreshold(samples, periodsPerYear: 8760);

        tau.ShouldBeGreaterThanOrEqualTo(0.80);
    }

    [Fact]
    public void PickBestThreshold_Picks_Low_Threshold_When_All_Signals_Are_Profitable()
    {
        // All signals (any confidence) are profitable → low τ wins (take everything).
        List<AdaptationSample> samples = new();
        for (int i = 0; i < 200; i++)
        {
            double conf = (i % 10) / 10d + 0.05;
            samples.Add(Synthetic.Sample(conf, TradeAction.Buy, 0.005));
        }

        double tau = ThresholdOptimizer.PickBestThreshold(samples, periodsPerYear: 8760);

        tau.ShouldBe(0.50);
    }

    [Fact]
    public void PickBestThreshold_Falls_Back_To_Default_On_Empty_Input()
    {
        double tau = ThresholdOptimizer.PickBestThreshold(Array.Empty<AdaptationSample>(), periodsPerYear: 8760);

        tau.ShouldBe(0.50);
    }

    [Fact]
    public void PickBestThreshold_Ignores_Hold_Signals_For_Pnl_Computation()
    {
        // HOLD signals have RealizedReturnPct = 0; they shouldn't bias τ. With only
        // HOLD samples, every τ produces a zero return series (Sharpe = 0); the
        // optimizer falls back to the first candidate.
        List<AdaptationSample> samples = new();
        for (int i = 0; i < 50; i++)
        {
            double conf = (i % 10) / 10d + 0.05;
            samples.Add(Synthetic.Sample(conf, TradeAction.Hold, 0));
        }

        double tau = ThresholdOptimizer.PickBestThreshold(samples, periodsPerYear: 8760);

        tau.ShouldBe(0.50);
    }
}
