using Shouldly;
using TradingSignal.Adaptation.MetaModel;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;

namespace TradingSignal.Adaptation.Tests;

public sealed class MetaModelStrategyTests
{
    private sealed class StubGenerator : ISignalGenerator
    {
        public Task<RawSignal> GenerateAsync(FeatureSet f, IReadOnlyList<FewShotCase> mem, CancellationToken ct)
            => Task.FromResult(new RawSignal(TradeAction.Buy, 0.5, "stub"));
    }

    private sealed class StubFeatureEngine : IFeatureEngine
    {
        public int WarmupPeriods => 0;
        public FeatureSet Compute(IReadOnlyList<Candle> candles, int upToIndex) => Synthetic.Features();
    }

    [Fact]
    public void Returns_Raw_Action_When_Untrained()
    {
        MetaModelStrategy sut = new(new StubFeatureEngine(), new StubGenerator());

        FinalDecision d = sut.Apply(new RawSignal(TradeAction.Buy, 0.7, "x"), Synthetic.Features());

        d.Action.ShouldBe(TradeAction.Buy);
    }

    [Fact]
    public void Skips_Training_When_Below_Minimum_Sample_Count()
    {
        MetaModelStrategy sut = new(new StubFeatureEngine(), new StubGenerator());

        // Train on too few samples → model stays untrained → passes raw through.
        List<AdaptationSample> samples = Enumerable.Range(0, 5)
            .Select(i => Synthetic.Sample(0.6, TradeAction.Buy, 0.01))
            .ToList();
        sut.TrainOnSamples(samples);

        FinalDecision d = sut.Apply(new RawSignal(TradeAction.Buy, 0.7, "x"), Synthetic.Features());

        d.Action.ShouldBe(TradeAction.Buy);
        sut.LastTrainAccuracy.ShouldBe(0d);
    }

    [Fact]
    public void Learns_Separable_Pattern()
    {
        // Pattern: when return5 > 0, BUY is profitable; when return5 < 0, BUY loses.
        // The meta-model should learn this and gate accordingly.
        List<AdaptationSample> samples = new();
        for (int i = 0; i < 200; i++)
        {
            double return5 = i % 2 == 0 ? 0.02 : -0.02;
            double pnl = return5 > 0 ? 0.01 : -0.01;
            samples.Add(Synthetic.Sample(0.7, TradeAction.Buy, pnl, return5: return5));
        }

        MetaModelStrategy sut = new(new StubFeatureEngine(), new StubGenerator());
        sut.TrainOnSamples(samples);

        sut.LastTrainAccuracy.ShouldBeGreaterThanOrEqualTo(0.80);

        FinalDecision dPositive = sut.Apply(
            new RawSignal(TradeAction.Buy, 0.7, "x"),
            Synthetic.Features(return5: 0.02));
        FinalDecision dNegative = sut.Apply(
            new RawSignal(TradeAction.Buy, 0.7, "x"),
            Synthetic.Features(return5: -0.02));

        dPositive.Action.ShouldBe(TradeAction.Buy);
        dNegative.Action.ShouldBe(TradeAction.Hold);
    }

    [Fact]
    public void Hold_Is_Always_Passed_Through_Unchanged()
    {
        MetaModelStrategy sut = new(new StubFeatureEngine(), new StubGenerator());

        // Train so the model is active
        List<AdaptationSample> samples = Enumerable.Range(0, 50)
            .Select(i => Synthetic.Sample(0.7, TradeAction.Buy, i % 2 == 0 ? 0.01 : -0.01))
            .ToList();
        sut.TrainOnSamples(samples);

        FinalDecision d = sut.Apply(new RawSignal(TradeAction.Hold, 0.5, "x"), Synthetic.Features());

        d.Action.ShouldBe(TradeAction.Hold);
    }
}
