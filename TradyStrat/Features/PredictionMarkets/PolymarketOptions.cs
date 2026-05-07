using Microsoft.Extensions.Configuration;

namespace TradyStrat.Features.PredictionMarkets;

public sealed record PolymarketOptions(
    string                BaseUrl,
    IReadOnlyList<string> Tags,
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
            Tags:           s.GetSection("Tags").Get<string[]>()
                              ?? ["bitcoin", "crypto", "coinbase", "ethereum"],
            MaxMarkets:     s.GetValue("MaxMarkets",     10),
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
