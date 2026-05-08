using Microsoft.Extensions.Configuration;

namespace TradyStrat.Features.PredictionMarkets;

/// <summary>
/// Polymarket pre-filter knobs. <see cref="SearchQueries"/> hits Gamma's
/// public-search endpoint (the tag-based market filter on the gamma /markets
/// endpoint is silently ignored — see PolymarketGammaProvider for context).
/// </summary>
public sealed record PolymarketOptions(
    string                BaseUrl,
    IReadOnlyList<string> SearchQueries,
    int                   MaxMarkets,
    decimal               MinVolumeUsd,
    int                   MaxHorizonDays);

public static class PolymarketOptionsBinder
{
    public static PolymarketOptions Read(IConfiguration cfg)
    {
        var s = cfg.GetSection("Polymarket");
        var opts = new PolymarketOptions(
            BaseUrl:        s["BaseUrl"] ?? "https://gamma-api.polymarket.com",
            SearchQueries:  s.GetSection("SearchQueries").Get<string[]>()
                              ?? s.GetSection("Tags").Get<string[]>()    // back-compat
                              ?? ["bitcoin", "ethereum", "coinbase", "fed"],
            MaxMarkets:     s.GetValue("MaxMarkets",     8),
            MinVolumeUsd:   s.GetValue("MinVolumeUsd",   50_000m),
            MaxHorizonDays: s.GetValue("MaxHorizonDays", 365));

        Validate(opts.MaxMarkets, opts.MinVolumeUsd, opts.MaxHorizonDays);
        return opts;
    }

    private static void Validate(int maxMarkets, decimal minVolumeUsd, int maxHorizonDays)
    {
        if (maxMarkets     <= 0) throw new ArgumentOutOfRangeException(nameof(maxMarkets),     maxMarkets,     "MaxMarkets must be > 0");
        if (minVolumeUsd   <  0) throw new ArgumentOutOfRangeException(nameof(minVolumeUsd),   minVolumeUsd,   "MinVolumeUsd must be >= 0");
        if (maxHorizonDays <= 0) throw new ArgumentOutOfRangeException(nameof(maxHorizonDays), maxHorizonDays, "MaxHorizonDays must be > 0");
    }
}
