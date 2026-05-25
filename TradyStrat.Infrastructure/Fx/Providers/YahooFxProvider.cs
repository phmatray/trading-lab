using TradyStrat.Application.Fx.Providers;
using System.Text.Json;
using TradyStrat.Domain;
using TradyStrat.Domain.MarketData;
using TradyStrat.Domain.Shared.Money;

namespace TradyStrat.Infrastructure.Fx.Providers;

public sealed class YahooFxProvider(HttpClient http) : IFxRateProvider
{
    public async Task<IReadOnlyList<FxRate>> FetchAsync(
        string @base, string quote, DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(@base) || string.IsNullOrWhiteSpace(quote))
            throw new FxRateUnavailableException("Base and quote must not be empty.");

        var symbol = $"{@base.ToUpperInvariant()}{quote.ToUpperInvariant()}=X";

        var p1 = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
        var p2 = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue),   TimeSpan.Zero).ToUnixTimeSeconds();
        var url = $"/v8/finance/chart/{symbol}?period1={p1}&period2={p2}&interval=1d";

        try
        {
            using var resp = await http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
                throw new FxRateUnavailableException($"Yahoo {(int)resp.StatusCode} for {symbol}");

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var first = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
            var ts    = first.GetProperty("timestamp");
            var close = first.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close");

            var fetchedAt = DateTime.UtcNow;
            var rates = new List<FxRate>(ts.GetArrayLength());
            for (var i = 0; i < ts.GetArrayLength(); i++)
            {
                if (close[i].ValueKind == JsonValueKind.Null) continue;
                var date = DateOnly.FromDateTime(
                    DateTimeOffset.FromUnixTimeSeconds(ts[i].GetInt64()).UtcDateTime);
                rates.Add(new FxRate(
                    date,
                    CurrencyPair.Of(
                        Currency.Parse(@base),
                        Currency.Parse(quote)),
                    (decimal)close[i].GetDouble(),
                    fetchedAt));
            }
            return rates;
        }
        catch (FxRateUnavailableException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                    or JsonException or KeyNotFoundException
                                    or InvalidOperationException)
        {
            throw new FxRateUnavailableException($"FX fetch failed for {symbol}", ex);
        }
    }
}
