using NodaTime;
using YahooQuotesApi;

namespace TradingStrat.Services;

public class YahooFinanceService : IYahooFinanceService
{
    public async Task<IReadOnlyList<HistoricalDataPoint>> GetHistoricalDataAsync(
        string ticker,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            // Convert DateTime to NodaTime Instant
            var startInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(startDate, DateTimeKind.Utc));

            // Create YahooQuotes with history start date
            var yahooQuotes = new YahooQuotesBuilder()
                .WithHistoryStartDate(startInstant)
                .Build();

            // Fetch historical data
            var result = await yahooQuotes.GetHistoryAsync(ticker).ConfigureAwait(false);

            if (!result.HasValue || result.Value.Ticks.IsEmpty)
            {
                return Array.Empty<HistoricalDataPoint>();
            }

            // Filter by end date and convert to our data structure
            var endInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(endDate, DateTimeKind.Utc));
            var dataPoints = result.Value.Ticks
                .Where(tick => tick.Date <= endInstant)
                .Select(tick => new HistoricalDataPoint(
                    DateTime: tick.Date.ToDateTimeUtc(),
                    Open: (decimal)tick.Open,
                    High: (decimal)tick.High,
                    Low: (decimal)tick.Low,
                    Close: (decimal)tick.Close,
                    AdjustedClose: (decimal)tick.AdjustedClose,
                    Volume: tick.Volume
                ))
                .ToList();

            return dataPoints;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to fetch historical data for {ticker}: {ex.Message}", ex);
        }
    }
}
