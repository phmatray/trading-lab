using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradyStrat.Cli.Mcp.Serialization;

internal sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString()
            ?? throw new JsonException("Expected a non-null ISO date string.");
        if (DateOnly.TryParseExact(s, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        throw new JsonException($"Invalid date '{s}' — use ISO {Format}.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}
