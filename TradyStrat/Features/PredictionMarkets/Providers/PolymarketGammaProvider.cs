using System.Text.Json;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.Time;

namespace TradyStrat.Features.PredictionMarkets.Providers;

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
