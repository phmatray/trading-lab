using System.Text.Json;
using System.Text.RegularExpressions;
using TradingSignal.Core;

namespace TradingSignal.Llm.Parsing;

public static class SignalResponseParser
{
    private static readonly Regex FencedJson = new(
        @"```(?:json)?\s*(\{[\s\S]*?\})\s*```",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryParse(string raw, out RawSignal signal)
    {
        signal = new RawSignal(TradeAction.Hold, 0d, "parse_failure");
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var jsonText = ExtractJson(raw);
        if (jsonText is null) return false;

        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object) return false;
            if (!TryReadAction(root, out var action)) return false;
            if (!root.TryGetProperty("confidence", out var confEl)) return false;
            if (confEl.ValueKind != JsonValueKind.Number) return false;

            var confidence = Clamp01(confEl.GetDouble());

            var reason = root.TryGetProperty("reason", out var reasonEl) && reasonEl.ValueKind == JsonValueKind.String
                ? reasonEl.GetString() ?? string.Empty
                : string.Empty;

            signal = new RawSignal(action, confidence, reason);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? ExtractJson(string raw)
    {
        var fenced = FencedJson.Match(raw);
        if (fenced.Success) return fenced.Groups[1].Value;

        var firstBrace = raw.IndexOf('{');
        var lastBrace = raw.LastIndexOf('}');
        if (firstBrace < 0 || lastBrace < firstBrace) return null;

        return raw.Substring(firstBrace, lastBrace - firstBrace + 1);
    }

    private static bool TryReadAction(JsonElement root, out TradeAction action)
    {
        action = TradeAction.Hold;
        if (!root.TryGetProperty("action", out var actionEl)) return false;
        if (actionEl.ValueKind != JsonValueKind.String) return false;

        var raw = actionEl.GetString();
        if (string.IsNullOrWhiteSpace(raw)) return false;

        return raw.Trim().ToUpperInvariant() switch
        {
            "BUY" => SetAndReturn(out action, TradeAction.Buy),
            "SELL" => SetAndReturn(out action, TradeAction.Sell),
            "HOLD" => SetAndReturn(out action, TradeAction.Hold),
            _ => false,
        };
    }

    private static bool SetAndReturn(out TradeAction slot, TradeAction value)
    {
        slot = value;
        return true;
    }

    private static double Clamp01(double v)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return 0d;
        if (v < 0d) return 0d;
        if (v > 1d) return 1d;
        return v;
    }
}
