using TradyStrat.Features.PriceFeed.Providers;
using TradyStrat.Domain;

namespace TradyStrat.Tests.PriceFeed.Providers;

public sealed class StubPriceFeed(IReadOnlyList<PriceBar> bars) : IPriceFeed
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

    public Task<InstrumentMetadata> GetInstrumentMetadataAsync(string ticker, CancellationToken ct)
        => throw new NotImplementedException();
}
