using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Infrastructure.Utilities;

namespace TradingStrat.Infrastructure.MarketData;

/// <summary>
/// Alpha Vantage API adapter for fetching intraday market data.
/// Supports M1, M5, M15, M30, H1 timeframes with automatic rate limiting.
/// </summary>
public class AlphaVantageAdapter : IMarketDataPort
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AlphaVantageAdapter> _logger;
    private readonly AlphaVantageSettings _settings;
    private readonly RateLimiter _rateLimiter;
    private const string BaseUrl = "https://www.alphavantage.co/query";

    public AlphaVantageAdapter(
        HttpClient httpClient,
        IOptions<TradingConfiguration> configuration,
        ILogger<AlphaVantageAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = configuration.Value.AlphaVantage;

        // Configure HttpClient timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        // Initialize rate limiter (5 calls per minute for free tier)
        _rateLimiter = new RateLimiter(
            maxCallsPerWindow: _settings.MaxCallsPerMinute,
            windowDuration: TimeSpan.FromMinutes(1));
    }

    public async Task<IReadOnlyList<HistoricalPrice>> FetchHistoricalDataAsync(
        string ticker,
        TimeFrame timeFrame,
        DateTime startDate,
        DateTime endDate)
    {
        // Validate API key is configured
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException(
                "Alpha Vantage API key is not configured. " +
                "Please set Trading:AlphaVantage:ApiKey in configuration.");
        }

        // Validate timeframe is intraday
        if (!timeFrame.IsIntraday())
        {
            throw new NotSupportedException(
                $"Alpha Vantage adapter only supports intraday timeframes. " +
                $"Use Yahoo Finance adapter for daily/weekly/monthly data. " +
                $"Requested timeframe: {timeFrame}");
        }

        _logger.LogInformation(
            "Fetching intraday data for {Ticker} ({TimeFrame}) from {StartDate} to {EndDate}",
            ticker, timeFrame, startDate, endDate);

        // Map TimeFrame to Alpha Vantage interval parameter
        string interval = MapTimeFrameToInterval(timeFrame);

        // Enforce rate limiting before API call
        await _rateLimiter.WaitForSlotAsync();

        // Build API URL with query parameters
        string url = $"{BaseUrl}?function=TIME_SERIES_INTRADAY&symbol={ticker}&interval={interval}" +
                     $"&outputsize=full&apikey={_settings.ApiKey}";

        // Execute API call with retries
        string jsonResponse = await FetchWithRetriesAsync(url);

        // Parse JSON response and convert to HistoricalPrice entities
        List<HistoricalPrice> prices = ParseIntradayResponse(jsonResponse, ticker, timeFrame);

        // Filter by date range (Alpha Vantage doesn't support date range parameters)
        List<HistoricalPrice> filtered = prices
            .Where(p => p.DateTime >= startDate && p.DateTime <= endDate)
            .OrderBy(p => p.DateTime)
            .ToList();

        _logger.LogInformation(
            "Fetched {Count} intraday data points for {Ticker} ({TimeFrame})",
            filtered.Count, ticker, timeFrame);

        return filtered;
    }

    private string MapTimeFrameToInterval(TimeFrame timeFrame)
    {
        return timeFrame.Unit switch
        {
            TimeFrameUnit.M1 => "1min",
            TimeFrameUnit.M5 => "5min",
            TimeFrameUnit.M15 => "15min",
            TimeFrameUnit.M30 => "30min",
            TimeFrameUnit.H1 => "60min",
            TimeFrameUnit.H4 => throw new NotSupportedException(
                "Alpha Vantage does not support 4-hour intervals. Maximum is 60min (H1)."),
            _ => throw new NotSupportedException($"Unsupported timeframe for Alpha Vantage: {timeFrame}")
        };
    }

    private async Task<string> FetchWithRetriesAsync(string url)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= _settings.MaxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("Alpha Vantage API request (attempt {Attempt}/{MaxRetries})",
                    attempt, _settings.MaxRetries);

                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();

                // Check for API error messages
                if (content.Contains("Error Message") || content.Contains("Note"))
                {
                    throw new InvalidOperationException(
                        $"Alpha Vantage API error: {ExtractErrorMessage(content)}");
                }

                return content;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex,
                    "Alpha Vantage API request failed (attempt {Attempt}/{MaxRetries})",
                    attempt, _settings.MaxRetries);

                if (attempt < _settings.MaxRetries)
                {
                    // Exponential backoff: 1s, 2s, 4s, ...
                    TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    await Task.Delay(delay);
                }
            }
        }

        throw new InvalidOperationException(
            $"Failed to fetch data from Alpha Vantage after {_settings.MaxRetries} attempts.",
            lastException);
    }

    private List<HistoricalPrice> ParseIntradayResponse(string jsonResponse, string ticker, TimeFrame timeFrame)
    {
        List<HistoricalPrice> prices = new();

        try
        {
            using JsonDocument document = JsonDocument.Parse(jsonResponse);
            JsonElement root = document.RootElement;

            // Find the time series property (varies by interval)
            JsonElement? timeSeries = FindTimeSeriesElement(root);

            if (timeSeries is null)
            {
                _logger.LogWarning("No time series data found in Alpha Vantage response");
                return prices;
            }

            // Parse each time series entry
            foreach (JsonProperty entry in timeSeries.Value.EnumerateObject())
            {
                string dateTimeStr = entry.Name;
                JsonElement values = entry.Value;

                if (!DateTime.TryParse(dateTimeStr, out DateTime dateTime))
                {
                    _logger.LogWarning("Failed to parse datetime: {DateTime}", dateTimeStr);
                    continue;
                }

                HistoricalPrice price = new()
                {
                    Ticker = ticker,
                    TimeFrame = timeFrame.Unit,
                    DateTime = dateTime,
                    Open = ParseDecimal(values, "1. open"),
                    High = ParseDecimal(values, "2. high"),
                    Low = ParseDecimal(values, "3. low"),
                    Close = ParseDecimal(values, "4. close"),
                    AdjustedClose = ParseDecimal(values, "4. close"), // Intraday doesn't have adjusted close
                    Volume = ParseLong(values, "5. volume")
                };

                prices.Add(price);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Alpha Vantage JSON response");
            throw new InvalidOperationException("Invalid JSON response from Alpha Vantage", ex);
        }

        return prices;
    }

    private JsonElement? FindTimeSeriesElement(JsonElement root)
    {
        // Alpha Vantage uses different property names like "Time Series (1min)", "Time Series (5min)", etc.
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (property.Name.StartsWith("Time Series", StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }

    private decimal? ParseDecimal(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement value) &&
            value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(value.GetString(), out decimal result))
        {
            return result;
        }

        return null;
    }

    private long? ParseLong(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement value) &&
            value.ValueKind == JsonValueKind.String &&
            long.TryParse(value.GetString(), out long result))
        {
            return result;
        }

        return null;
    }

    private string ExtractErrorMessage(string jsonResponse)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(jsonResponse);
            JsonElement root = document.RootElement;

            if (root.TryGetProperty("Error Message", out JsonElement errorMsg))
            {
                return errorMsg.GetString() ?? "Unknown error";
            }

            if (root.TryGetProperty("Note", out JsonElement note))
            {
                return note.GetString() ?? "Rate limit exceeded";
            }
        }
        catch
        {
            // If parsing fails, return the first 200 characters
        }

        return jsonResponse.Length > 200
            ? jsonResponse[..200] + "..."
            : jsonResponse;
    }

    public async Task<HistoricalPrice?> FetchLatestPriceAsync(string ticker)
    {
        try
        {
            // Fetch last hour of 1-minute data to get most recent price
            DateTime endDate = DateTime.Now;
            DateTime startDate = endDate.AddHours(-1);

            IReadOnlyList<HistoricalPrice> data = await FetchHistoricalDataAsync(
                ticker,
                TimeFrame.M1,
                startDate,
                endDate);

            // Return the most recent data point
            return data.OrderByDescending(d => d.DateTime).FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch latest intraday price for {Ticker}", ticker);
            throw new InvalidOperationException(
                $"Failed to fetch latest intraday price for {ticker}: {ex.Message}", ex);
        }
    }
}
