using TradyStrat.Domain;

namespace TradyStrat.Application.PriceFeed.Providers;

public interface IPriceFeed
{
    Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
        string ticker, DateOnly from, DateOnly to, CancellationToken ct);

    /// <summary>
    /// Probes the upstream provider (Yahoo) for instrument metadata. Returns a
    /// Probed Instrument with Id = InstrumentId.New() (zero sentinel) and
    /// AddedAt = default — the caller invokes Confirm(clock) before persisting.
    /// </summary>
    Task<Instrument> ProbeAsync(string ticker, CancellationToken ct);
}
