using System.Globalization;
using System.Text.Json;

namespace TradyStrat.Features.PredictionMarkets.Providers;

public static class PolymarketNormalizer
{
    public static IEnumerable<PredictionMarket> Normalize(JsonElement array)
    {
        if (array.ValueKind != JsonValueKind.Array) yield break;

        foreach (var el in array.EnumerateArray())
        {
            if (TryNormalize(el, out var market)) yield return market!;
        }
    }

    private static bool TryNormalize(JsonElement el, out PredictionMarket? market)
    {
        market = null;

        if (!el.TryGetProperty("slug",          out var slugEl)
         || !el.TryGetProperty("question",      out var questionEl)
         || !el.TryGetProperty("outcomes",      out var outcomesEl)
         || !el.TryGetProperty("outcomePrices", out var pricesEl)
         || !el.TryGetProperty("endDate",       out var endDateEl)
         || !el.TryGetProperty("volume",        out var volumeEl))
            return false;

        // outcomes and outcomePrices are stringified JSON arrays in Gamma's payload.
        if (!TryParseStringifiedArray(outcomesEl, out var outcomes)) return false;
        if (!TryParseStringifiedArray(pricesEl,   out var prices))   return false;

        // Phase 1: binary YES/NO only.
        if (outcomes.Count != 2 || outcomes[0] != "Yes" || outcomes[1] != "No") return false;
        if (prices.Count   != 2)                                                 return false;

        if (!decimal.TryParse(prices[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var yes) ||
            !decimal.TryParse(prices[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var no))
            return false;

        // Sanity: YES + NO must be ~1 (orderbook tick tolerance).
        if (Math.Abs(yes + no - 1m) > 0.01m) return false;

        if (!endDateEl.TryGetDateTime(out var endDateTime)) return false;
        var endDate = DateOnly.FromDateTime(endDateTime);

        var volumeStr = volumeEl.ValueKind == JsonValueKind.String ? volumeEl.GetString() : volumeEl.GetRawText();
        if (!decimal.TryParse(volumeStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var volume) || volume < 0)
            return false;

        var tags = new List<string>();
        if (el.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in tagsEl.EnumerateArray())
                if (t.TryGetProperty("slug", out var tagSlugEl) && tagSlugEl.GetString() is { } s)
                    tags.Add(s);
        }

        market = new PredictionMarket(
            Slug:        slugEl.GetString()     ?? "",
            Question:    questionEl.GetString() ?? "",
            Probability: yes,
            EndDate:     endDate,
            VolumeUsd:   volume,
            Tags:        tags);
        return true;
    }

    private static bool TryParseStringifiedArray(JsonElement el, out List<string> items)
    {
        items = [];
        if (el.ValueKind != JsonValueKind.String) return false;
        var raw = el.GetString();
        if (string.IsNullOrEmpty(raw)) return false;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return false;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                items.Add(item.ValueKind == JsonValueKind.String ? item.GetString() ?? "" : item.GetRawText());
            }
            return true;
        }
        catch (JsonException) { return false; }
    }
}
