using TradyStrat.Application.PriceFeed.Providers;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;

namespace TradyStrat.TestKit;

public sealed class StubPriceFeed(IReadOnlyList<PriceBar> bars, IClock? clock = null) : IPriceFeed
{
    public int CallCount { get; private set; }
    public List<(DateOnly From, DateOnly To)> Ranges { get; } = new();

    public Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
        string ticker, DateOnly from, DateOnly to, CancellationToken ct)
    {
        CallCount++;
        Ranges.Add((from, to));
        return Task.FromResult(bars);
    }

    public Task<Instrument> ProbeAsync(string ticker, CancellationToken ct)
        => Task.FromResult(Instrument.Probed(
            ticker:     ticker,
            name:       $"Stub {ticker}",
            currency:   Currency.Eur,
            exchange:   Exchange.Of("STUB"),
            timezoneId: TimezoneId.Of("UTC"),
            kind:       InstrumentKind.Held,
            now:        clock?.UtcNow() ?? DateTime.UtcNow));
}
