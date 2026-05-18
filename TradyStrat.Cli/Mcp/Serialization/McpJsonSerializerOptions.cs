using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace TradyStrat.Cli.Mcp.Serialization;

internal static class McpJsonSerializerOptions
{
    public static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            // Required in .NET 10+: the MCP SDK calls MakeReadOnly() on the options when
            // registering tools and that throws if no TypeInfoResolver is set.
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        o.Converters.Add(new JsonStringEnumConverter());
        o.Converters.Add(new DateOnlyJsonConverter());
        return o;
    }
}
