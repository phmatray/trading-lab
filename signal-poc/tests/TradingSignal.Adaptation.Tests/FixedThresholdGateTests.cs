using Shouldly;
using TradingSignal.Core;

namespace TradingSignal.Adaptation.Tests;

public sealed class FixedThresholdGateTests
{
    [Fact]
    public void Passes_Through_Above_Threshold()
    {
        FixedThresholdGate sut = new(threshold: 0.6);

        FinalDecision d = sut.Apply(new RawSignal(TradeAction.Buy, 0.7, "x"), Synthetic.Features());

        d.Action.ShouldBe(TradeAction.Buy);
    }

    [Fact]
    public void Gates_To_Hold_Below_Threshold()
    {
        FixedThresholdGate sut = new(threshold: 0.6);

        FinalDecision d = sut.Apply(new RawSignal(TradeAction.Sell, 0.5, "x"), Synthetic.Features());

        d.Action.ShouldBe(TradeAction.Hold);
    }

    [Fact]
    public void Label_Reflects_Threshold()
    {
        FixedThresholdGate sut = new(threshold: 0.65);

        sut.Label.ShouldContain("0.65");
    }
}
