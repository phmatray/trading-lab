using Shouldly;
using TradyStrat.Common.Domain;
using Xunit;

namespace TradyStrat.Tests.Common.Domain;

public class IndicatorKindParserTests
{
    [Theory]
    [InlineData("RSI(14)",   IndicatorKind.Rsi)]
    [InlineData("RSI",       IndicatorKind.Rsi)]
    [InlineData("Bollinger", IndicatorKind.Bollinger)]
    [InlineData("Ichimoku",  IndicatorKind.Ichimoku)]
    [InlineData("200-SMA",   IndicatorKind.Sma200)]
    [InlineData("SMA-200",   IndicatorKind.Sma200)]
    [InlineData("50-SMA",    IndicatorKind.Sma50)]
    public void Maps_known_labels(string label, IndicatorKind expected)
        => IndicatorKindParser.From(label).ShouldBe(expected);

    [Theory]
    [InlineData("MACD")]
    [InlineData("")]
    [InlineData("unknown")]
    public void Returns_null_for_unknown(string label)
        => IndicatorKindParser.From(label).ShouldBeNull();

    [Fact]
    public void Null_input_returns_null()
        => IndicatorKindParser.From(null!).ShouldBeNull();
}
