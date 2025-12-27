using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Services;

/// <summary>
/// Application service for fetching current market prices.
/// Centralizes price fetching logic to eliminate duplication across use cases.
/// </summary>
public class MarketPriceService
{
    private const int PriceLookbackDays = 7;

    /// <summary>
    /// Fetches current prices for multiple tickers.
    /// </summary>
    /// <param name="tickers">The list of tickers to fetch prices for.</param>
    /// <param name="marketDataPort">The market data port to use for fetching.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>A result containing a dictionary of ticker to price, or errors if any occurred.</returns>
    public async Task<Result<Dictionary<string, decimal>>> GetCurrentPricesAsync(
        IEnumerable<string> tickers,
        IMarketDataPort marketDataPort,
        IProgress<string>? progress = null)
    {
        var prices = new Dictionary<string, decimal>();
        var errors = new List<Error>();

        foreach (string ticker in tickers)
        {
            progress?.Report($"Fetching price for {ticker}...");

            Result<decimal> result = await GetLatestPriceAsync(ticker, marketDataPort);

            if (result.IsSuccess)
            {
                prices[ticker] = result.Value;
            }
            else
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors.Any()
            ? Result<Dictionary<string, decimal>>.Failure(errors)
            : Result<Dictionary<string, decimal>>.Success(prices);
    }

    /// <summary>
    /// Fetches the current price for a single ticker.
    /// </summary>
    /// <param name="ticker">The ticker to fetch price for.</param>
    /// <param name="marketDataPort">The market data port to use for fetching.</param>
    /// <returns>A result containing the latest closing price, or an error if not available.</returns>
    public async Task<Result<decimal>> GetLatestPriceAsync(
        string ticker,
        IMarketDataPort marketDataPort)
    {
        try
        {
            // Fetch recent data (last 7 days to ensure we get the latest price)
            IReadOnlyList<HistoricalPrice> historicalData = await marketDataPort.FetchHistoricalDataAsync(
                ticker,
                TimeFrame.D1,
                DateTime.Today.AddDays(-PriceLookbackDays),
                DateTime.Today);

            if (!historicalData.Any())
            {
                return Result<decimal>.Failure(
                    Error.NotFound(
                        $"No recent price data available for {ticker}",
                        "PRICE_DATA_NOT_FOUND"));
            }

            // Get most recent closing price
            HistoricalPrice latestPrice = historicalData
                .OrderByDescending(p => p.DateTime)
                .First();

            if (!latestPrice.Close.HasValue)
            {
                return Result<decimal>.Failure(
                    Error.Validation(
                        $"No closing price available for {ticker}",
                        "CLOSING_PRICE_MISSING"));
            }

            return Result<decimal>.Success(latestPrice.Close.Value);
        }
        catch (Exception ex)
        {
            return Result<decimal>.Failure(
                Error.Validation(
                    $"Failed to fetch price for {ticker}: {ex.Message}",
                    "PRICE_FETCH_FAILED"));
        }
    }
}
