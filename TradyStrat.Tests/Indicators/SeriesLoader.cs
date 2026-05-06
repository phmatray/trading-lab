using System.Globalization;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Tests.Indicators;

public static class SeriesLoader
{
    public static IReadOnlyList<PriceBar> LoadCloses(string ticker = "X")
    {
        var path = Path.Combine("Indicators", "Fixtures", "sample-closes.csv");
        var lines = File.ReadAllLines(path).Skip(1);
        var bars = new List<PriceBar>();
        foreach (var line in lines)
        {
            var parts = line.Split(',');
            var date  = DateOnly.Parse(parts[0], CultureInfo.InvariantCulture);
            var close = decimal.Parse(parts[1], CultureInfo.InvariantCulture);
            bars.Add(new PriceBar
            {
                Id = 0, Ticker = ticker, Date = date,
                Open = close, High = close, Low = close, Close = close, Volume = 1
            });
        }
        return bars;
    }
}
