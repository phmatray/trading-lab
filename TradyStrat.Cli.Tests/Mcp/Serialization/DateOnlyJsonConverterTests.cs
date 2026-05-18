using System.Text.Json;
using Shouldly;
using TradyStrat.Cli.Mcp.Serialization;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Serialization;

public class DateOnlyJsonConverterTests
{
    private static JsonSerializerOptions Opts() =>
        new() { Converters = { new DateOnlyJsonConverter() } };

    [Fact]
    public void Serializes_to_iso_yyyy_mm_dd()
    {
        var json = JsonSerializer.Serialize(new DateOnly(2026, 5, 18), Opts());
        json.ShouldBe("\"2026-05-18\"");
    }

    [Fact]
    public void Roundtrips_iso_string_to_dateonly()
    {
        var d = JsonSerializer.Deserialize<DateOnly>("\"2026-05-18\"", Opts());
        d.ShouldBe(new DateOnly(2026, 5, 18));
    }

    [Fact]
    public void Rejects_unparseable_string()
    {
        ((Action)(() => JsonSerializer.Deserialize<DateOnly>("\"not-a-date\"", Opts()))).ShouldThrow<JsonException>();
    }
}
