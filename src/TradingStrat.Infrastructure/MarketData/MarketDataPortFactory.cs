using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Infrastructure.MarketData;

/// <summary>
/// Factory for selecting the appropriate market data adapter based on timeframe.
/// - Yahoo Finance: Daily, Weekly, Monthly (D1, W1, MN1)
/// - Alpha Vantage: Intraday (M1, M5, M15, M30, H1)
/// </summary>
public class MarketDataPortFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketDataPortFactory> _logger;

    public MarketDataPortFactory(
        IServiceProvider serviceProvider,
        ILogger<MarketDataPortFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the appropriate market data adapter for the specified timeframe.
    /// </summary>
    /// <param name="timeFrame">The timeframe for which to fetch data</param>
    /// <returns>IMarketDataPort implementation suitable for the timeframe</returns>
    /// <exception cref="NotSupportedException">Thrown when timeframe is not supported by any adapter</exception>
    public IMarketDataPort GetAdapter(TimeFrame timeFrame)
    {
        _logger.LogDebug("Selecting market data adapter for timeframe: {TimeFrame}", timeFrame);

        IMarketDataPort adapter = timeFrame.Unit switch
        {
            // Intraday timeframes → Alpha Vantage
            TimeFrameUnit.M1 or
            TimeFrameUnit.M5 or
            TimeFrameUnit.M15 or
            TimeFrameUnit.M30 or
            TimeFrameUnit.H1 => GetAlphaVantageAdapter(),

            // H4 not supported by either provider
            TimeFrameUnit.H4 => throw new NotSupportedException(
                "4-hour (H4) timeframe is not natively supported by any data provider. " +
                "Consider using H1 data and aggregating, or use D1 for daily analysis."),

            // Daily and higher timeframes → Yahoo Finance
            TimeFrameUnit.D1 or
            TimeFrameUnit.W1 or
            TimeFrameUnit.MN1 => GetYahooFinanceAdapter(),

            _ => throw new NotSupportedException($"Unsupported timeframe: {timeFrame}")
        };

        _logger.LogInformation("Selected {AdapterType} for timeframe {TimeFrame}",
            adapter.GetType().Name, timeFrame);

        return adapter;
    }

    private IMarketDataPort GetAlphaVantageAdapter()
    {
        // Resolve AlphaVantageAdapter from DI container
        IMarketDataPort? adapter = _serviceProvider.GetService<AlphaVantageAdapter>();

        if (adapter is null)
        {
            throw new InvalidOperationException(
                "AlphaVantageAdapter is not registered in DI container. " +
                "Ensure it's registered in ServiceCollectionExtensions.");
        }

        return adapter;
    }

    private IMarketDataPort GetYahooFinanceAdapter()
    {
        // Resolve YahooFinanceAdapter from DI container
        IMarketDataPort? adapter = _serviceProvider.GetService<YahooFinanceAdapter>();

        if (adapter is null)
        {
            throw new InvalidOperationException(
                "YahooFinanceAdapter is not registered in DI container. " +
                "Ensure it's registered in ServiceCollectionExtensions.");
        }

        return adapter;
    }
}
