using Shouldly;
using TradyStrat.Application.Trades;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Portfolio;
using Xunit;

namespace TradyStrat.Application.Tests.Trades;

public class CsvImportServiceTests
{
    [Fact]
    public void Parses_minimum_columns_without_ticker()
    {
        const string csv = """
            date,side,qty,price,fees
            2026-05-01,buy,10,123.45,1.99
            """;

        var rows = CsvImportService.Parse(new StringReader(csv));

        rows.Count.ShouldBe(1);
        rows[0].ExecutedOn.ShouldBe(new DateOnly(2026, 5, 1));
        rows[0].Side.ShouldBe(TradeSide.Buy);
        rows[0].Quantity.ShouldBe(10m);
        rows[0].PricePerShare.ShouldBe(123.45m);
        rows[0].FeesEur.ShouldBe(1.99m);
        rows[0].Ticker.ShouldBeNull();
    }

    [Fact]
    public void Reads_ticker_column_when_present()
    {
        const string csv = """
            date,side,qty,price,fees,ticker
            2026-05-01,buy,10,123.45,1.99,CON3.L
            2026-05-02,sell,5,200.00,0.50,COIN
            """;

        var rows = CsvImportService.Parse(new StringReader(csv));

        rows.Count.ShouldBe(2);
        rows[0].Ticker.ShouldBe("CON3.L");
        rows[1].Ticker.ShouldBe("COIN");
    }

    [Fact]
    public void Empty_ticker_value_throws_with_line_context()
    {
        const string csv = """
            date,side,qty,price,fees,ticker
            2026-05-01,buy,10,123.45,1.99,
            """;

        var ex = Should.Throw<CsvImportException>(() =>
            CsvImportService.Parse(new StringReader(csv)));

        ex.LineNumber.ShouldBe(2);
    }

    [Fact]
    public void Ticker_column_order_is_independent_of_other_columns()
    {
        const string csv = """
            ticker,date,side,qty,price,fees
            COIN,2026-05-01,buy,10,123.45,1.99
            """;

        var rows = CsvImportService.Parse(new StringReader(csv));

        rows[0].Ticker.ShouldBe("COIN");
        rows[0].ExecutedOn.ShouldBe(new DateOnly(2026, 5, 1));
    }
}
