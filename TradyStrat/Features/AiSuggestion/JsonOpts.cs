using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradyStrat.Features.AiSuggestion;

public static class JsonOpts
{
    public static readonly JsonSerializerOptions Strict = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };
}
