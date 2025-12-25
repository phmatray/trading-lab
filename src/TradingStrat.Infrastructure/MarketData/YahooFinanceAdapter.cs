using NodaTime;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;
using YahooQuotesApi;

namespace TradingStrat.Infrastructure.MarketData;

public class YahooFinanceAdapter : IMarketDataPort
{
    public async Task<IReadOnlyList<HistoricalPrice>> FetchHistoricalDataAsync(
        string ticker,
        TimeFrame timeFrame,
        DateTime startDate,
        DateTime endDate)
    {
        // Yahoo Finance only supports daily, weekly, and monthly data
        if (timeFrame.IsIntraday())
        {
            throw new NotSupportedException(
                $"Yahoo Finance does not support intraday timeframes ({timeFrame}). " +
                "Please use Alpha Vantage adapter for intraday data (M1, M5, M15, M30, H1, H4).");
        }

        try
        {
            // Convert DateTime to NodaTime Instant
            var startInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(startDate, DateTimeKind.Utc));

            // Create YahooQuotes with history start date
            YahooQuotes yahooQuotes = new YahooQuotesBuilder()
                .WithHistoryStartDate(startInstant)
                .Build();

            // Fetch historical data
            Result<History> result = await yahooQuotes.GetHistoryAsync(ticker).ConfigureAwait(false);

            if (!result.HasValue || result.Value.Ticks.IsEmpty)
            {
                return Array.Empty<HistoricalPrice>();
            }

            // Filter by end date and convert to domain entities
            var endInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(endDate, DateTimeKind.Utc));
            var dataPoints = result.Value.Ticks
                .Where(tick => tick.Date <= endInstant)
                .Select(tick => new HistoricalPrice
                {
                    Ticker = ticker,
                    DateTime = tick.Date.ToDateTimeUtc(),
                    Open = (decimal)tick.Open,
                    High = (decimal)tick.High,
                    Low = (decimal)tick.Low,
                    Close = (decimal)tick.Close,
                    AdjustedClose = (decimal)tick.AdjustedClose,
                    Volume = tick.Volume,
                    TimeFrame = timeFrame.Unit,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            return dataPoints;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to fetch historical data for {ticker}: {ex.Message}", ex);
        }
    }

    public async Task<HistoricalPrice?> FetchLatestPriceAsync(string ticker)
    {
        try
        {
            // Fetch last 7 days to ensure we get at least one data point
            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.AddDays(-7);

            IReadOnlyList<HistoricalPrice> data = await FetchHistoricalDataAsync(ticker, TimeFrame.D1, startDate, endDate);

            // Return the most recent data point
            return data.OrderByDescending(d => d.DateTime).FirstOrDefault();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to fetch latest price for {ticker}: {ex.Message}", ex);
        }
    }
}
