using Shouldly;
using TradyStrat.Application.Indicators.History;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Application.Indicators.Bollinger;
using TradyStrat.Application.Indicators.Ichimoku;
using TradyStrat.Application.Indicators.MovingAverage;
using TradyStrat.Application.Indicators.Rsi;
using Xunit;

namespace TradyStrat.Tests.Indicators.History;

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
