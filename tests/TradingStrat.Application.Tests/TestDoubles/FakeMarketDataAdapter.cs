using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Tests.TestDoubles;

/// <summary>
/// Fake implementation of IMarketDataPort for testing.
/// Returns predictable fake data - no external API calls.
/// </summary>
public class FakeMarketDataAdapter : IMarketDataPort
{
    private readonly List<HistoricalPrice> _fakeData;

    public FakeMarketDataAdapter(List<HistoricalPrice>? data = null)
    {
        _fakeData = data ?? GenerateDefaultFakeData();
    }

    public Task<IReadOnlyList<HistoricalPrice>> FetchHistoricalDataAsync(
        string ticker,
        DateTime startDate,
        DateTime endDate)
    {
        var filtered = _fakeData
            .Where(p => p.DateTime >= startDate && p.DateTime <= endDate)
            .Select(p => new HistoricalPrice
            {
                Ticker = ticker,
                DateTime = p.DateTime,
                Open = p.Open,
                High = p.High,
                Low = p.Low,
                Close = p.Close,
                AdjustedClose = p.AdjustedClose,
                Volume = p.Volume
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<HistoricalPrice>>(filtered);
    }

    public Task<HistoricalPrice?> FetchLatestPriceAsync(string ticker)
    {
        var latest = _fakeData.OrderByDescending(p => p.DateTime).FirstOrDefault();

        if (latest == null)
        {
            return Task.FromResult<HistoricalPrice?>(null);
        }

        return Task.FromResult<HistoricalPrice?>(new HistoricalPrice
        {
            Ticker = ticker,
            DateTime = latest.DateTime,
            Open = latest.Open,
            High = latest.High,
            Low = latest.Low,
            Close = latest.Close,
            AdjustedClose = latest.AdjustedClose,
            Volume = latest.Volume
        });
    }

    private static List<HistoricalPrice> GenerateDefaultFakeData()
    {
        var data = new List<HistoricalPrice>();
        var baseDate = new DateTime(2024, 1, 1);
        var basePrice = 100m;

        // Generate 100 days of fake data with slight upward trend
        for (int i = 0; i < 100; i++)
        {
            var date = baseDate.AddDays(i);
            var open = basePrice + i * 0.5m;
            var close = open + (decimal)(new Random(i).NextDouble() - 0.5) * 2m;
            var high = Math.Max(open, close) + 1m;
            var low = Math.Min(open, close) - 1m;

            data.Add(new HistoricalPrice
            {
                Ticker = "DEFAULT",
                DateTime = date,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                AdjustedClose = close,
                Volume = 1000000 + i * 10000
            });
        }

        return data;
    }
}
