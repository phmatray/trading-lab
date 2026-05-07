using Shouldly;
using TradyStrat.Features.Indicators;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
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
