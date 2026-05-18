using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradyStrat.Cli.Mcp.Serialization;

internal static class McpJsonSerializerOptions
{
    public static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };
        o.Converters.Add(new JsonStringEnumConverter());
        o.Converters.Add(new DateOnlyJsonConverter());
        return o;
    }
}
