using System.Globalization;
using System.Text.Json;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.Time;

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

public static class PolymarketFilter
{
    public static IReadOnlyList<PredictionMarket> Apply(
        IEnumerable<PredictionMarket> markets,
        DateOnly today,
        decimal minVolumeUsd,
        int maxHorizonDays,
        int maxMarkets)
    {
        var horizon = today.AddDays(maxHorizonDays);
        var seen = new HashSet<string>();
        var deduped = new List<PredictionMarket>();
        foreach (var m in markets)
            if (seen.Add(m.Slug))
                deduped.Add(m);

        return deduped
            .Where(m => m.VolumeUsd >= minVolumeUsd)
            .Where(m => m.EndDate <= horizon)
            .OrderByDescending(m => m.VolumeUsd)
            .Take(maxMarkets)
            .ToList();
    }
}

public sealed class PolymarketGammaProvider(
    HttpClient http,
    PolymarketOptions options,
    IClock clock) : IPredictionMarketProvider
{
    public async Task<IReadOnlyList<PredictionMarket>> GetMarketsAsync(CancellationToken ct)
    {
        // Fan out: one request per tag, all in parallel. Any failure aborts.
        var perTag = options.Tags
            .Select(tag => FetchTagAsync(tag, ct))
            .ToArray();

        IReadOnlyList<PredictionMarket>[] results;
        try
        {
            results = await Task.WhenAll(perTag);
        }
        catch (PolymarketUnavailableException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new PolymarketUnavailableException("Polymarket fetch failed.", ex);
        }

        var merged = results.SelectMany(r => r);
        var today = DateOnly.FromDateTime(clock.UtcNow().Date);
        return PolymarketFilter.Apply(merged, today, options.MinVolumeUsd, options.MaxHorizonDays, options.MaxMarkets);
    }

    private async Task<IReadOnlyList<PredictionMarket>> FetchTagAsync(string tag, CancellationToken ct)
    {
        // Over-fetch buffer so post-filter has room to drop multi-outcome / out-of-horizon rows.
        var limit = options.MaxMarkets * 2;
        var url = $"/markets?active=true&closed=false&order=volume&ascending=false&limit={limit}&tag_slug={Uri.EscapeDataString(tag)}";

        try
        {
            using var resp = await http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
                throw new PolymarketUnavailableException($"Gamma {(int)resp.StatusCode} for tag {tag}");

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return PolymarketNormalizer.Normalize(doc.RootElement).ToList();
        }
        catch (PolymarketUnavailableException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new PolymarketUnavailableException($"Polymarket fetch failed for tag {tag}", ex);
        }
    }
}
