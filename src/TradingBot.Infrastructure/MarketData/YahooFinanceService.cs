// <copyright file="YahooFinanceService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.MarketData;
using YahooFinanceApi;
using YahooCandle = YahooFinanceApi.Candle;

namespace TradingBot.Infrastructure.MarketData;

/// <summary>
/// Yahoo Finance service with Polly resilience policies.
/// </summary>
public sealed class YahooFinanceService : IMarketDataService
{
    private readonly ILogger<YahooFinanceService> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly Dictionary<string, Action<Quote>> _subscriptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooFinanceService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public YahooFinanceService(ILogger<YahooFinanceService> logger)
    {
        _logger = logger;
        _subscriptions = new Dictionary<string, Action<Quote>>();
        _resiliencePipeline = CreateResiliencePipeline();
    }

    /// <inheritdoc/>
    public async Task<Quote> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching quote for {Symbol}", symbol);

        var securities = await _resiliencePipeline.ExecuteAsync(
            async token => await Yahoo.Symbols(symbol).QueryAsync(token),
            cancellationToken);

        var security = securities[symbol];

        return new Quote
        {
            Symbol = symbol,
            Timestamp = DateTime.UtcNow,
            Price = (decimal)security.RegularMarketPrice,
            Bid = (decimal)security.Bid,
            Ask = (decimal)security.Ask,
            Volume = security.RegularMarketVolume,
            Change = (decimal)security.RegularMarketChange,
            ChangePercent = (decimal)security.RegularMarketChangePercent,
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default)
    {
        var symbolList = symbols.ToList();
        _logger.LogInformation("Fetching quotes for {Count} symbols", symbolList.Count);

        var quotes = new List<Quote>();

        // Fetch quotes in parallel with rate limiting
        var tasks = symbolList.Select(symbol => GetQuoteAsync(symbol, cancellationToken));
        var results = await Task.WhenAll(tasks);

        quotes.AddRange(results);

        return quotes;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Core.Models.MarketData.Candle>> GetHistoricalDataAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string timeframe,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching historical data for {Symbol} from {Start} to {End} ({Timeframe})",
            symbol,
            startDate,
            endDate,
            timeframe);

        var period = MapTimeframeToPeriod(timeframe);
        var history = await _resiliencePipeline.ExecuteAsync(
            async token => await Yahoo.GetHistoricalAsync(symbol, startDate, endDate, period, token),
            cancellationToken);

        return history.Select(h => new Core.Models.MarketData.Candle
        {
            Symbol = symbol,
            Timestamp = h.DateTime,
            Open = (decimal)h.Open,
            High = (decimal)h.High,
            Low = (decimal)h.Low,
            Close = (decimal)h.Close,
            Volume = h.Volume,
            Timeframe = timeframe,
        }).ToList();
    }

    /// <inheritdoc/>
    public Task SubscribeToQuotesAsync(
        string symbol,
        Action<Quote> callback,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Subscribing to quotes for {Symbol}", symbol);

        if (_subscriptions.ContainsKey(symbol))
        {
            _logger.LogWarning("Already subscribed to {Symbol}", symbol);
            return Task.CompletedTask;
        }

        _subscriptions[symbol] = callback;

        // Start background task to poll for updates
        _ = Task.Run(async () => await PollQuotesAsync(symbol, callback, cancellationToken), cancellationToken);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UnsubscribeFromQuotesAsync(string symbol)
    {
        _logger.LogInformation("Unsubscribing from quotes for {Symbol}", symbol);

        _subscriptions.Remove(symbol);

        return Task.CompletedTask;
    }

    private static Period MapTimeframeToPeriod(string timeframe)
    {
        return timeframe.ToLowerInvariant() switch
        {
            "1d" => Period.Daily,
            "1w" => Period.Weekly,
            "1mo" => Period.Monthly,
            _ => Period.Daily,
        };
    }

    private ResiliencePipeline CreateResiliencePipeline()
    {
        var retryOptions = new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            OnRetry = args =>
            {
                _logger.LogWarning(
                    "Retry attempt {Attempt} after {Delay}ms due to: {Exception}",
                    args.AttemptNumber,
                    args.RetryDelay.TotalMilliseconds,
                    args.Outcome.Exception?.Message);
                return ValueTask.CompletedTask;
            },
        };

        var circuitBreakerOptions = new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30),
            OnOpened = args =>
            {
                _logger.LogError("Circuit breaker opened due to: {Exception}", args.Outcome.Exception?.Message);
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                _logger.LogInformation("Circuit breaker closed");
                return ValueTask.CompletedTask;
            },
        };

        return new ResiliencePipelineBuilder()
            .AddRetry(retryOptions)
            .AddCircuitBreaker(circuitBreakerOptions)
            .Build();
    }

    private async Task PollQuotesAsync(string symbol, Action<Quote> callback, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _subscriptions.ContainsKey(symbol))
        {
            try
            {
                var quote = await GetQuoteAsync(symbol, cancellationToken);
                callback(quote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling quotes for {Symbol}", symbol);
            }

            // Poll every 5 seconds
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }
}
