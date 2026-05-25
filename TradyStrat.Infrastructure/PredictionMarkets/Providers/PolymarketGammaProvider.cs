using TradyStrat.Domain.Suggestions;
using TradyStrat.Application.PredictionMarkets;
using System.Text.Json;
using TradyStrat.Domain;
using TradyStrat.Application.Settings;

namespace TradyStrat.Infrastructure.PredictionMarkets.Providers;

public sealed class PolymarketGammaProvider(
    HttpClient http,
    IPolymarketSettingsRepository polymarket,
    IClock clock) : IPredictionMarketProvider
{
    // Gamma's tag-based market filter (`/markets?tag_slug=...`) is silently
    // ignored — it returns the global market firehose regardless. The
    // /public-search endpoint is the only Gamma API that respects a topical
    // filter; it returns events grouped above their constituent markets.
    public async Task<IReadOnlyList<PredictionMarket>> GetMarketsAsync(CancellationToken ct)
    {
        var opts = await polymarket.GetAsync(ct);

        var perQuery = opts.SearchQueries.Values
            .Select(q => FetchQueryAsync(q, opts.MaxMarkets.Value, ct))
            .ToArray();

        IReadOnlyList<PredictionMarket>[] results;
        try
        {
            results = await Task.WhenAll(perQuery);
        }
        catch (PolymarketUnavailableException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new PolymarketUnavailableException("Polymarket fetch failed.", ex);
        }

        var merged = results.SelectMany(r => r);
        var today = DateOnly.FromDateTime(clock.UtcNow().Date);
        return PolymarketFilter.Apply(merged, today, opts.MinVolumeUsd.Value, opts.MaxHorizonDays.Value, opts.MaxMarkets.Value);
    }

    private async Task<IReadOnlyList<PredictionMarket>> FetchQueryAsync(string query, int maxMarkets, CancellationToken ct)
    {
        // Over-fetch event-level buffer so post-filter (relevance + volume + horizon)
        // has room to drop noisy markets without starving the final list.
        var limit = maxMarkets * 2;
        var url = $"/public-search?q={Uri.EscapeDataString(query)}&limit={limit}";

        try
        {
            using var resp = await http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
                throw new PolymarketUnavailableException($"Gamma {(int)resp.StatusCode} for query '{query}'");

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            // Shape: { events: [ { markets: [ ...market objects... ] } ], pagination: {...} }
            var collected = new List<PredictionMarket>();
            if (doc.RootElement.TryGetProperty("events", out var events) && events.ValueKind == JsonValueKind.Array)
            {
                foreach (var ev in events.EnumerateArray())
                {
                    if (!ev.TryGetProperty("markets", out var markets) || markets.ValueKind != JsonValueKind.Array)
                        continue;
                    foreach (var market in PolymarketNormalizer.Normalize(markets))
                        if (PolymarketRelevance.IsRelevant(market.Question))
                            collected.Add(market);
                }
            }
            return collected;
        }
        catch (PolymarketUnavailableException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new PolymarketUnavailableException($"Polymarket fetch failed for query '{query}'", ex);
        }
    }
}
