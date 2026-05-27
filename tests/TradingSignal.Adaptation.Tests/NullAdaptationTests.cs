using Shouldly;
using TradingSignal.Adaptation;
using TradingSignal.Core;

namespace TradingSignal.Adaptation.Tests;

public sealed class NullAdaptationTests
{
    [Fact]
    public void Passes_Action_Through_Unchanged()
    {
        NullAdaptation sut = new();

        sut.Apply(new RawSignal(TradeAction.Buy, 0.1, "x"), Synthetic.Features()).Action.ShouldBe(TradeAction.Buy);
        sut.Apply(new RawSignal(TradeAction.Sell, 0.9, "x"), Synthetic.Features()).Action.ShouldBe(TradeAction.Sell);
        sut.Apply(new RawSignal(TradeAction.Hold, 0.5, "x"), Synthetic.Features()).Action.ShouldBe(TradeAction.Hold);
    }
}
