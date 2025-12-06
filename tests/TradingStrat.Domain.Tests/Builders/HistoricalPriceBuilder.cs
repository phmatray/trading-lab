using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Tests.Builders;

/// <summary>
/// Test builder for creating HistoricalPrice test data.
/// Provides fluent API for creating test scenarios.
/// </summary>
public class HistoricalPriceBuilder
{
    private string _ticker = "TEST";
    private DateTime _startDate = new(2024, 1, 1);
    private List<decimal> _closePrices = new();
    private long _baseVolume = 1000000;

    public static HistoricalPriceBuilder Create() => new();

    public HistoricalPriceBuilder WithTicker(string ticker)
    {
        _ticker = ticker;
        return this;
    }

    public HistoricalPriceBuilder WithStartDate(DateTime startDate)
    {
        _startDate = startDate;
        return this;
    }

    public HistoricalPriceBuilder WithPrices(params decimal[] prices)
    {
        _closePrices = prices.ToList();
        return this;
    }

    public HistoricalPriceBuilder WithTrendingPrices(int count, decimal startPrice, decimal increment)
    {
        _closePrices = Enumerable.Range(0, count)
            .Select(i => startPrice + i * increment)
            .ToList();
        return this;
    }

    public HistoricalPriceBuilder WithVolume(long baseVolume)
    {
        _baseVolume = baseVolume;
        return this;
    }

    public List<HistoricalPrice> Build()
    {
        var result = new List<HistoricalPrice>();

        for (int i = 0; i < _closePrices.Count; i++)
        {
            decimal close = _closePrices[i];
            decimal open = i > 0 ? _closePrices[i - 1] : close * 0.99m;
            decimal high = Math.Max(open, close) * 1.01m;
            decimal low = Math.Min(open, close) * 0.99m;

            result.Add(new HistoricalPrice
            {
                Ticker = _ticker,
                DateTime = _startDate.AddDays(i),
                Open = open,
                High = high,
                Low = low,
                Close = close,
                AdjustedClose = close,
                Volume = _baseVolume + i * 10000
            });
        }

        return result;
    }

    public HistoricalPrice BuildSingle()
    {
        List<HistoricalPrice> prices = Build();
        return prices.First();
    }
}
