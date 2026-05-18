using TradyStrat.Common.Exceptions;
using Xunit;
using Shouldly;
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Tests.Common.Exceptions;

public class ExceptionHierarchyTests
{
    [Fact]
    public void All_typed_exceptions_derive_from_TradyStratException()
    {
        new PriceFeedUnavailableException("x").ShouldBeAssignableTo<TradyStratException>();
        new FxRateUnavailableException("x").ShouldBeAssignableTo<TradyStratException>();
        new AnthropicCallFailedException("x").ShouldBeAssignableTo<TradyStratException>();
        new AnthropicConfigurationException("x").ShouldBeAssignableTo<TradyStratException>();
        new IndicatorComputationException("x").ShouldBeAssignableTo<TradyStratException>();
        new TradeValidationException("x").ShouldBeAssignableTo<TradyStratException>();
        new CsvImportException("x", lineNumber: 7).ShouldBeAssignableTo<TradyStratException>();
        new NoTradingDaysException("x").ShouldBeAssignableTo<TradyStratException>();
    }

    [Fact]
    public void CsvImportException_prefixes_line_number()
    {
        var ex = new CsvImportException("bad row", lineNumber: 42);

        ex.Message.ShouldBe("line 42: bad row");
        ex.LineNumber.ShouldBe(42);
    }

    [Fact]
    public void CsvImportException_omits_prefix_when_no_line_number()
    {
        new CsvImportException("bad file").Message.ShouldBe("bad file");
    }
}
