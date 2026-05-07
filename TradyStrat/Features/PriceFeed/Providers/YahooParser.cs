using System.Text.Json;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;

namespace TradyStrat.Features.PriceFeed.Providers;

public static class YahooParser
{
    public static IReadOnlyList<PriceBar> ParseDaily(string ticker, JsonDocument doc)
    {
        try
        {
            var root = doc.RootElement.GetProperty("chart");
            if (root.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.Object)
                throw new PriceFeedUnavailableException(
                    $"Yahoo error for {ticker}: {err.GetRawText()}");

            var result = root.GetProperty("result");
            if (result.ValueKind != JsonValueKind.Array || result.GetArrayLength() == 0)
                throw new PriceFeedUnavailableException($"Yahoo returned no result for {ticker}");

            var first = result[0];
            if (!first.TryGetProperty("timestamp", out var ts) || ts.ValueKind != JsonValueKind.Array)
                return [];

            var quote   = first.GetProperty("indicators").GetProperty("quote")[0];
            var opens   = quote.GetProperty("open");
            var highs   = quote.GetProperty("high");
            var lows    = quote.GetProperty("low");
            var closes  = quote.GetProperty("close");
            var volumes = quote.GetProperty("volume");

            var bars = new List<PriceBar>(ts.GetArrayLength());
            for (var i = 0; i < ts.GetArrayLength(); i++)
            {
                if (closes[i].ValueKind == JsonValueKind.Null) continue;

                var date = DateOnly.FromDateTime(
                    DateTimeOffset.FromUnixTimeSeconds(ts[i].GetInt64()).UtcDateTime);

                bars.Add(new PriceBar
                {
                    Id     = 0,
                    Ticker = ticker,
                    Date   = date,
                    Open   = AsDecimal(opens[i]),
                    High   = AsDecimal(highs[i]),
                    Low    = AsDecimal(lows[i]),
                    Close  = AsDecimal(closes[i]),
                    Volume = volumes[i].ValueKind == JsonValueKind.Null ? 0L : volumes[i].GetInt64(),
                });
            }
            return bars;
        }
        catch (PriceFeedUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException
                                       or FormatException or JsonException)
        {
            throw new PriceFeedUnavailableException(
                $"Failed to parse Yahoo payload for {ticker}", ex);
        }
    }

    private static decimal AsDecimal(JsonElement e)
        => e.ValueKind == JsonValueKind.Null ? 0m : (decimal)e.GetDouble();

    public static InstrumentMetadata ParseMetadata(string ticker, JsonDocument doc)
    {
        try
        {
            var root = doc.RootElement.GetProperty("quoteResponse");
            if (root.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.Object)
                throw new PriceFeedUnavailableException(
                    $"Yahoo error for {ticker}: {err.GetRawText()}");

            var result = root.GetProperty("result");
            if (result.ValueKind != JsonValueKind.Array || result.GetArrayLength() == 0)
                throw new InstrumentNotFoundException(
                    $"Yahoo returned no quote for '{ticker}'.");

            var first = result[0];
            var name = ReadString(first, "longName")
                    ?? ReadString(first, "shortName")
                    ?? throw new InstrumentMetadataIncompleteException(
                           $"Yahoo response for '{ticker}' has no longName or shortName.");

            var currency = ReadString(first, "currency")
                    ?? throw new InstrumentMetadataIncompleteException(
                           $"Yahoo response for '{ticker}' has no currency.");

            var exchange = ReadString(first, "fullExchangeName")
                    ?? throw new InstrumentMetadataIncompleteException(
                           $"Yahoo response for '{ticker}' has no fullExchangeName.");

            var tz = ReadString(first, "exchangeTimezoneName")
                    ?? throw new InstrumentMetadataIncompleteException(
                           $"Yahoo response for '{ticker}' has no exchangeTimezoneName.");

            return new InstrumentMetadata(ticker, name, currency, exchange, tz);
        }
        catch (TradyStratException) { throw; }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException
                                    or JsonException)
        {
            throw new PriceFeedUnavailableException(
                $"Failed to parse Yahoo metadata payload for {ticker}", ex);
        }
    }

    private static string? ReadString(JsonElement el, string name)
        => el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}
