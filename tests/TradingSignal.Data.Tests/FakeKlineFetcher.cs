using TradingSignal.Core;
using TradingSignal.Data.Binance;

namespace TradingSignal.Data.Tests;

internal sealed class FakeKlineFetcher : IKlineFetcher
{
    private readonly IReadOnlyList<Candle> _all;
    public int CallCount { get; private set; }
    public List<DateTime> Cursors { get; } = new();

    public FakeKlineFetcher(IReadOnlyList<Candle> all)
    {
        _all = all;
    }

    public Task<IReadOnlyList<Candle>> FetchPageAsync(
        string symbol, TimeSpan interval, DateTime startUtc, DateTime endUtc, int limit, CancellationToken ct)
    {
        CallCount++;
        Cursors.Add(startUtc);
        var page = _all
            .Where(c => c.OpenTimeUtc >= startUtc && c.OpenTimeUtc < endUtc)
            .Take(limit)
            .ToList();
        return Task.FromResult<IReadOnlyList<Candle>>(page);
    }
}
