using System.Text.Json;

namespace TradingSignal.Llm.Schemas;

internal static class SignalResponseSchema
{
    public const string SchemaName = "SignalResponse";
    public const string SchemaDescription = "A single trade decision with confidence and brief rationale.";

    public const string Json = """
        {
          "type": "object",
          "properties": {
            "action": { "type": "string", "enum": ["BUY", "SELL", "HOLD"] },
            "confidence": { "type": "number", "minimum": 0.0, "maximum": 1.0 },
            "reason": { "type": "string", "minLength": 1, "maxLength": 240 }
          },
          "required": ["action", "confidence", "reason"],
          "additionalProperties": false
        }
        """;

    private static JsonElement? _element;

    public static JsonElement Element
    {
        get
        {
            if (_element is not null) return _element.Value;
            using var doc = JsonDocument.Parse(Json);
            _element = doc.RootElement.Clone();
            return _element.Value;
        }
    }
}
