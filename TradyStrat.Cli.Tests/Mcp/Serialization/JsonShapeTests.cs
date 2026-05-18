using System.Text.Json;
using Shouldly;
using TradyStrat.Cli.Mcp.Serialization;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Serialization;

public class JsonShapeTests
{
    private static readonly JsonSerializerOptions Opts = McpJsonSerializerOptions.Create();

    [Fact]
    public void DateOnly_is_iso_yyyy_mm_dd()
    {
        var json = JsonSerializer.Serialize(new { date = new DateOnly(2026, 5, 18) }, Opts);
        json.ShouldContain("\"date\":\"2026-05-18\"");
    }

    [Fact]
    public void Decimal_is_json_number_not_string()
    {
        var json = JsonSerializer.Serialize(new { price = 24.13m }, Opts);
        json.ShouldContain("\"price\":24.13");
        json.ShouldNotContain("\"24.13\"");
    }

    [Fact]
    public void Property_names_are_camelCase()
    {
        var json = JsonSerializer.Serialize(new { MarketValueEur = 100m }, Opts);
        json.ShouldContain("marketValueEur");
    }

    private enum SampleEnum { Acquire, Hold }

    [Fact]
    public void Enums_are_pascalcase_strings()
    {
        var json = JsonSerializer.Serialize(new { action = SampleEnum.Acquire }, Opts);
        json.ShouldContain("\"action\":\"Acquire\"");
    }

    [Fact]
    public void Null_propagates_explicitly()
    {
        var json = JsonSerializer.Serialize(new { forwardReturnPct = (decimal?)null }, Opts);
        json.ShouldContain("\"forwardReturnPct\":null");
    }
}
