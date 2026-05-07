using System.Text.Json;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;

namespace TradyStrat.Features.PriceFeed.Providers;

public sealed class YahooPriceFeed(HttpClient http) : IPriceFeed
{
    public async Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
        string ticker, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var p1 = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
        var p2 = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue),   TimeSpan.Zero).ToUnixTimeSeconds();
        var url = $"/v8/finance/chart/{Uri.EscapeDataString(ticker)}?period1={p1}&period2={p2}&interval=1d";

        try
        {
            using var resp = await http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
                throw new PriceFeedUnavailableException(
                    $"Yahoo {(int)resp.StatusCode} for {ticker}");

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return YahooParser.ParseDaily(ticker, doc);
        }
        catch (PriceFeedUnavailableException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new PriceFeedUnavailableException($"Yahoo fetch failed for {ticker}", ex);
        }
    }

    public async Task<InstrumentMetadata> GetInstrumentMetadataAsync(
        string ticker, CancellationToken ct)
    {
        var url = $"/v7/finance/quote?symbols={Uri.EscapeDataString(ticker)}";

        try
        {
            using var resp = await http.GetAsync(url, ct);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InstrumentNotFoundException(
                    $"Yahoo 404 for '{ticker}'.");
            if (!resp.IsSuccessStatusCode)
                throw new PriceFeedUnavailableException(
                    $"Yahoo {(int)resp.StatusCode} for {ticker}");

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return YahooParser.ParseMetadata(ticker, doc);
        }
        catch (TradyStratException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new PriceFeedUnavailableException($"Yahoo metadata fetch failed for {ticker}", ex);
        }
    }
}
