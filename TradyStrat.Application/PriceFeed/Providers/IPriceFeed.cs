using TradyStrat.Domain;

namespace TradyStrat.Application.PriceFeed.Providers;

public interface IPriceFeed
{
    Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
        string ticker, DateOnly from, DateOnly to, CancellationToken ct);

    Task<InstrumentMetadata> GetInstrumentMetadataAsync(
        string ticker, CancellationToken ct);
}
