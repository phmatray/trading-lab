using Shouldly;
using TradyStrat.Features.Trades;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using Xunit;

namespace TradyStrat.Tests.Trades;

public class CsvImportServiceTests
{
    [Fact]
    public void Parses_header_then_rows()
    {
        const string csv = "date,side,qty,price,fees\n2026-05-01,Buy,10,4.20,0.50\n2026-05-03,Sell,5,4.80,0.50\n";
        var rows = CsvImportService.Parse(new StringReader(csv));

        rows.Count.ShouldBe(2);
        rows[0].ExecutedOn.ShouldBe(new DateOnly(2026,5,1));
        rows[0].Side.ShouldBe(TradeSide.Buy);
        rows[0].Quantity.ShouldBe(10m);
        rows[1].PricePerShare.ShouldBe(4.80m);
    }

    [Fact]
    public void Rejects_unknown_side()
    {
        const string csv = "date,side,qty,price,fees\n2026-05-01,Hold,1,1,0\n";
        var ex = Should.Throw<CsvImportException>(() => CsvImportService.Parse(new StringReader(csv)));
        ex.LineNumber.ShouldBe(2);
    }

    [Fact]
    public void Rejects_missing_columns()
    {
        const string csv = "date,side,qty\n2026-05-01,Buy,10\n";
        Should.Throw<CsvImportException>(() => CsvImportService.Parse(new StringReader(csv)));
    }

    [Fact]
    public void Rejects_blank_input()
    {
        Should.Throw<CsvImportException>(() => CsvImportService.Parse(new StringReader("")));
    }

    [Fact]
    public void Tolerates_lowercase_headers_and_whitespace()
    {
        const string csv = "  Date , Side , Qty , Price , Fees \n2026-05-01,buy,10,4.20,0\n";
        var rows = CsvImportService.Parse(new StringReader(csv));
        rows.Count.ShouldBe(1);
    }
}
