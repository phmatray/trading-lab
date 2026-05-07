using Shouldly;
using TradyStrat.Features.Indicators.History;
using TradyStrat.Features.Indicators;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.Indicators.Bollinger;
using TradyStrat.Features.Indicators.Ichimoku;
using TradyStrat.Features.Indicators.MovingAverage;
using TradyStrat.Features.Indicators.Rsi;
using Xunit;

namespace TradyStrat.Tests.Indicators;

public class IndicatorHistoryProviderFactoryTests
{
    private static IndicatorHistoryProviderFactory Factory() =>
        new([
            new RsiHistoryProvider(),
            new BollingerHistoryProvider(),
            new IchimokuHistoryProvider(),
            new Sma200HistoryProvider(),
        ]);

    [Theory]
    [InlineData(IndicatorKind.Rsi,       typeof(RsiHistoryProvider))]
    [InlineData(IndicatorKind.Bollinger, typeof(BollingerHistoryProvider))]
    [InlineData(IndicatorKind.Ichimoku,  typeof(IchimokuHistoryProvider))]
    [InlineData(IndicatorKind.Sma200,    typeof(Sma200HistoryProvider))]
    public void Resolves_concrete_strategy(IndicatorKind kind, Type expected)
        => Factory().For(kind).GetType().ShouldBe(expected);

    [Fact]
    public void Throws_for_unregistered_kind()
        => Should.Throw<IndicatorComputationException>(
            () => Factory().For(IndicatorKind.Sma50));
}
